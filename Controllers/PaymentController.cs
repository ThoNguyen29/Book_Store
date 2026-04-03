using System.Security.Claims;
using System.Text.Json;
using Book_Store.Models;
using Book_Store.Models.Momo;
using Book_Store.Services.Momo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Book_Store.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IMomoService _momoService;
        private readonly ILogger<PaymentController> _logger;
        private readonly ApplicationDbContext _context;

        private const string CartKeyPrefix = "cart_user_";
        private const string PendingMomoCheckoutPrefix = "momo_checkout_";

        public PaymentController(
            IMomoService momoService,
            ILogger<PaymentController> logger,
            ApplicationDbContext context)
        {
            _momoService = momoService;
            _logger = logger;
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePaymentMomo(OrderInfoModel model, string address)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return LocalRedirect("/Account/Login?returnUrl=%2FCart%2FCheckout");
            }

            var cart = GetCart(userId);
            if (!cart.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            if (string.IsNullOrWhiteSpace(address))
            {
                TempData["MomoError"] = "Vui lòng nhập địa chỉ giao hàng trước khi thanh toán MoMo.";
                return RedirectToAction("Checkout", "Cart");
            }

            var user = _context.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.ID == userId);

            var customerName = !string.IsNullOrWhiteSpace(user?.FullName)
                ? user.FullName
                : user?.Email ?? User.FindFirstValue(ClaimTypes.Name) ?? "Khách hàng";

            model.Amount = cart.Sum(x => x.Price * x.Quantity).ToString("0");
            model.OrderInfo = string.IsNullOrWhiteSpace(model.OrderInfo)
                ? "Thanh toán đơn hàng"
                : model.OrderInfo;
            model.FullName = customerName;
            model.OrderId = DateTime.UtcNow.Ticks.ToString();

            SavePendingMomoCheckout(new PendingMomoCheckoutModel
            {
                MomoOrderId = model.OrderId,
                UserId = userId,
                CustomerName = customerName,
                ShippingAddress = address.Trim(),
                TotalAmount = cart.Sum(x => x.Price * x.Quantity),
                CartItems = cart
                    .Select(x => new CartItem
                    {
                        ProductId = x.ProductId,
                        ProductName = x.ProductName,
                        Price = x.Price,
                        Quantity = x.Quantity,
                        Image = x.Image
                    })
                    .ToList()
            });

            try
            {
                var response = await _momoService.CreatePaymentMomo(model);
                if (response?.PayUrl == null)
                {
                    var momoMessage = !string.IsNullOrWhiteSpace(response?.LocalMessage)
                        ? response!.LocalMessage
                        : response?.Message;

                    TempData["MomoError"] = string.IsNullOrWhiteSpace(momoMessage)
                        ? $"Không tạo được liên kết thanh toán MoMo. Mã lỗi: {response?.ErrorCode}."
                        : $"Không tạo được liên kết thanh toán MoMo ({response?.ErrorCode}): {momoMessage}";

                    _logger.LogWarning(
                        "MoMo create payment failed. ErrorCode: {ErrorCode}, Message: {Message}",
                        response?.ErrorCode,
                        momoMessage);

                    return RedirectToAction("Checkout", "Cart");
                }

                _logger.LogInformation("MoMo payUrl generated: {PayUrl}", response.PayUrl);
                return Redirect(response.PayUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception when creating MoMo payment link.");
                TempData["MomoError"] = "Không kết nối được cổng thanh toán MoMo. Vui lòng thử lại.";
                return RedirectToAction("Checkout", "Cart");
            }
        }

        [HttpGet]
        public IActionResult PaymentCallBack()
        {
            var response = _momoService.PaymentExecuteAsync(HttpContext.Request.Query);

            if (response.IsSuccess && response.SignatureValid)
            {
                response.LocalOrderId = CreateOrderFromSuccessfulMomoPayment(response);
            }

            return View(response);
        }

        [HttpGet]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult MomoNotify()
        {
            _logger.LogInformation(
                "Received MoMo notify callback. Method: {Method}, QueryString: {QueryString}",
                Request.Method,
                Request.QueryString.Value);

            return Ok(new { message = "MoMo notify received" });
        }

        private int? CreateOrderFromSuccessfulMomoPayment(MomoExecuteResponseModel response)
        {
            if (string.IsNullOrWhiteSpace(response.OrderId))
            {
                return null;
            }

            var pendingCheckout = GetPendingMomoCheckout(response.OrderId);
            if (pendingCheckout == null)
            {
                _logger.LogWarning("No pending MoMo checkout found for OrderId {OrderId}", response.OrderId);
                return null;
            }

            if (pendingCheckout.CreatedOrderId.HasValue)
            {
                return pendingCheckout.CreatedOrderId.Value;
            }

            if (!decimal.TryParse(response.Amount, out var paidAmount) || paidAmount != pendingCheckout.TotalAmount)
            {
                _logger.LogWarning(
                    "MoMo amount mismatch for OrderId {OrderId}. Callback: {PaidAmount}, Pending: {PendingAmount}",
                    response.OrderId,
                    response.Amount,
                    pendingCheckout.TotalAmount);
                return null;
            }

            var order = new Order
            {
                UserID = pendingCheckout.UserId,
                CustomerName = pendingCheckout.CustomerName,
                ShippingAddress = pendingCheckout.ShippingAddress,
                PaymentMethod = "MoMo",
                OrderDate = DateTime.Now,
                Status = OrderStatus.Pending,
                TotalAmount = pendingCheckout.TotalAmount
            };

            _context.Orders.Add(order);
            _context.SaveChanges();

            foreach (var item in pendingCheckout.CartItems)
            {
                _context.OrderDetails.Add(new OrderDetail
                {
                    OrderID = order.OrderID,
                    BookID = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Price
                });
            }

            _context.Payments.Add(new Payment
            {
                OrderID = order.OrderID,
                Method = "MoMo",
                Status = "Paid",
                PaidAt = DateTime.Now
            });

            _context.SaveChanges();

            HttpContext.Session.Remove(GetCartKey(pendingCheckout.UserId));

            pendingCheckout.CreatedOrderId = order.OrderID;
            SavePendingMomoCheckout(pendingCheckout);

            return order.OrderID;
        }

        private bool TryGetCurrentUserId(out int userId)
        {
            userId = 0;
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out userId);
        }

        private List<CartItem> GetCart(int userId)
        {
            var session = HttpContext.Session.GetString(GetCartKey(userId));

            if (!string.IsNullOrEmpty(session))
            {
                return JsonSerializer.Deserialize<List<CartItem>>(session)
                    ?? new List<CartItem>();
            }

            return new List<CartItem>();
        }

        private PendingMomoCheckoutModel? GetPendingMomoCheckout(string momoOrderId)
        {
            var sessionValue = HttpContext.Session.GetString(GetPendingMomoCheckoutKey(momoOrderId));
            if (string.IsNullOrEmpty(sessionValue))
            {
                return null;
            }

            return JsonSerializer.Deserialize<PendingMomoCheckoutModel>(sessionValue);
        }

        private void SavePendingMomoCheckout(PendingMomoCheckoutModel model)
        {
            HttpContext.Session.SetString(
                GetPendingMomoCheckoutKey(model.MomoOrderId),
                JsonSerializer.Serialize(model));
        }

        private static string GetCartKey(int userId) => $"{CartKeyPrefix}{userId}";

        private static string GetPendingMomoCheckoutKey(string momoOrderId) => $"{PendingMomoCheckoutPrefix}{momoOrderId}";
    }
}
