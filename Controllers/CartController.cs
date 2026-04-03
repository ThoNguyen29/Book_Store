using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using Book_Store.Models;
using Book_Store.ViewModel.Cart;

namespace Book_Store.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string CartKeyPrefix = "cart_user_";

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return LocalRedirect("/gio-hang");
        }

        public IActionResult AddToCart(int id, int qty = 1)
        {
            if (!TryGetCurrentUserId(out _))
            {
                return BuildLoginRequiredJson();
            }

            var cart = GetCart();
            if (qty < 1)
            {
                qty = 1;
            }

            var book = _context.Books
                .Include(b => b.BookImages)
                .FirstOrDefault(b => b.BookID == id);

            if (book == null)
            {
                var emptyCount = cart.Sum(x => x.Quantity);
                var emptyTotal = cart.Sum(x => x.Price * x.Quantity);
                return Json(new { success = false, count = emptyCount, total = emptyTotal });
            }

            var item = cart.FirstOrDefault(p => p.ProductId == id);

            if (item == null)
            {
                cart.Add(new CartItem
                {
                    ProductId = book.BookID,
                    ProductName = book.Title,
                    Price = book.Price,
                    Image = book.BookImages?
                                .FirstOrDefault(i => i.IsPrimary)?.ImagePath
                                ?? "/images/no-image.png",
                    Quantity = qty
                });
            }
            else
            {
                item.Quantity += qty;
            }

            SaveCart(cart);

            var count = cart.Sum(x => x.Quantity);
            var total = cart.Sum(x => x.Price * x.Quantity);
            return Json(new { success = true, count, total });
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int id, int quantity)
        {
            if (!TryGetCurrentUserId(out _))
            {
                return RedirectToLogin();
            }

            var cart = GetCart();
            var item = cart.FirstOrDefault(p => p.ProductId == id);

            if (item != null && quantity > 0)
            {
                item.Quantity = quantity;
            }

            SaveCart(cart);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult UpdateQuantityAjax(int id, int quantity)
        {
            if (!TryGetCurrentUserId(out _))
            {
                return BuildLoginRequiredJson();
            }

            var cart = GetCart();
            var item = cart.FirstOrDefault(p => p.ProductId == id);

            if (item == null)
            {
                return Json(BuildCartResponse(cart, removed: true));
            }

            if (quantity <= 0)
            {
                cart.RemoveAll(p => p.ProductId == id);
                SaveCart(cart);
                return Json(BuildCartResponse(cart, removed: true));
            }

            item.Quantity = quantity;
            SaveCart(cart);
            return Json(BuildCartResponse(cart, item));
        }

        [HttpPost]
        public IActionResult IncreaseQuantity(int id)
        {
            if (!TryGetCurrentUserId(out _))
            {
                return RedirectToLogin();
            }

            var cart = GetCart();
            var item = cart.FirstOrDefault(p => p.ProductId == id);

            if (item != null)
            {
                item.Quantity += 1;
                SaveCart(cart);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult DecreaseQuantity(int id)
        {
            if (!TryGetCurrentUserId(out _))
            {
                return RedirectToLogin();
            }

            var cart = GetCart();
            var item = cart.FirstOrDefault(p => p.ProductId == id);

            if (item != null)
            {
                item.Quantity = Math.Max(1, item.Quantity - 1);
                SaveCart(cart);
            }

            return RedirectToAction("Index");
        }

        public IActionResult Remove(int id)
        {
            if (!TryGetCurrentUserId(out _))
            {
                return RedirectToLogin();
            }

            var cart = GetCart();
            cart.RemoveAll(p => p.ProductId == id);
            SaveCart(cart);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult RemoveAjax(int id)
        {
            if (!TryGetCurrentUserId(out _))
            {
                return BuildLoginRequiredJson();
            }

            var cart = GetCart();
            var exists = cart.Any(p => p.ProductId == id);

            if (exists)
            {
                cart.RemoveAll(p => p.ProductId == id);
                SaveCart(cart);
            }

            return Json(BuildCartResponse(cart, removed: exists));
        }

        [HttpPost]
        public IActionResult RemoveItemForm(int id)
        {
            return Remove(id);
        }

        public IActionResult Checkout()
        {
            if (!TryGetCurrentUserId(out _))
            {
                return RedirectToLogin();
            }

            var cart = GetCart();
            return View(cart);
        }

        [HttpPost]
        public IActionResult PlaceOrder(string address, string paymentMethod)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return RedirectToLogin();
            }

            var cart = GetCart();
            if (!cart.Any())
            {
                return RedirectToAction("Index");
            }

            address = address?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(address))
            {
                TempData["CheckoutError"] = "Vui lòng nhập địa chỉ giao hàng.";
                return RedirectToAction("Checkout");
            }

            var normalizedPaymentMethod = string.IsNullOrWhiteSpace(paymentMethod)
                ? "COD"
                : paymentMethod.Trim();

            var user = _context.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.ID == userId);

            var customerName = !string.IsNullOrWhiteSpace(user?.FullName)
                ? user.FullName
                : user?.Email ?? User.FindFirstValue(ClaimTypes.Name) ?? "Khách hàng";

            var totalAmount = cart.Sum(x => x.Price * x.Quantity);

            if (string.Equals(normalizedPaymentMethod, "MoMo", StringComparison.OrdinalIgnoreCase))
            {
                var redirectModel = new MomoCheckoutRedirectViewModel
                {
                    Address = address,
                    FullName = customerName,
                    Amount = totalAmount,
                    OrderInfo = "Thanh toán đơn hàng"
                };

                return View("RedirectToMomo", redirectModel);
            }

            if (!string.Equals(normalizedPaymentMethod, "COD", StringComparison.OrdinalIgnoreCase))
            {
                TempData["CheckoutError"] = "Phương thức thanh toán không hợp lệ.";
                return RedirectToAction("Checkout");
            }

            var order = new Order
            {
                UserID = userId,
                CustomerName = customerName,
                OrderDate = DateTime.Now,
                ShippingAddress = address,
                PaymentMethod = "COD",
                Status = OrderStatus.Pending,
                TotalAmount = totalAmount
            };

            _context.Orders.Add(order);
            _context.SaveChanges();

            foreach (var item in cart)
            {
                _context.OrderDetails.Add(new OrderDetail
                {
                    OrderID = order.OrderID,
                    BookID = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Price
                });
            }

            _context.SaveChanges();

            HttpContext.Session.Remove(GetCartKey(userId));

            return RedirectToAction("Success");
        }

        public IActionResult Success()
        {
            return View();
        }

        private List<CartItem> GetCart()
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return new List<CartItem>();
            }

            var session = HttpContext.Session.GetString(GetCartKey(userId));

            if (!string.IsNullOrEmpty(session))
            {
                return JsonSerializer.Deserialize<List<CartItem>>(session)
                    ?? new List<CartItem>();
            }

            return new List<CartItem>();
        }

        private void SaveCart(List<CartItem> cart)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return;
            }

            HttpContext.Session.SetString(
                GetCartKey(userId),
                JsonSerializer.Serialize(cart)
            );
        }

        private bool TryGetCurrentUserId(out int userId)
        {
            userId = 0;
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out userId);
        }

        private IActionResult RedirectToLogin()
        {
            var returnUrl = ResolveReturnUrl();
            return LocalRedirect(BuildLoginUrl(returnUrl));
        }

        private IActionResult BuildLoginRequiredJson()
        {
            var returnUrl = ResolveReturnUrl();
            var loginUrl = BuildLoginUrl(returnUrl);

            return Json(new
            {
                success = false,
                requiresLogin = true,
                loginUrl,
                count = 0,
                total = 0,
                quantity = 0,
                removed = false
            });
        }

        private string ResolveReturnUrl()
        {
            var referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrWhiteSpace(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
            {
                return refererUri.PathAndQuery;
            }

            return $"{Request.Path}{Request.QueryString}";
        }

        private static string BuildLoginUrl(string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                return "/Account/Login";
            }

            return $"/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl)}";
        }

        private static string GetCartKey(int userId) => $"{CartKeyPrefix}{userId}";

        private object BuildCartResponse(List<CartItem> cart, CartItem? item = null, bool removed = false, bool success = true)
        {
            var count = cart.Sum(x => x.Quantity);
            var total = cart.Sum(x => x.Price * x.Quantity);
            var quantity = item?.Quantity ?? 0;

            return new
            {
                success,
                count,
                total,
                quantity,
                removed
            };
        }
    }
}
