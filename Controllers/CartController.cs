using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using Book_Store.Models;

namespace Book_Store.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string CARTKEY = "cart";

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }

        public IActionResult AddToCart(int id, int qty = 1)
        {
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
                return Json(new { count = emptyCount, total = emptyTotal });
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
            return Json(new { count, total });
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int id, int quantity)
        {
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

        public IActionResult Remove(int id)
        {
            var cart = GetCart();
            cart.RemoveAll(p => p.ProductId == id);
            SaveCart(cart);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult RemoveAjax(int id)
        {
            var cart = GetCart();
            var exists = cart.Any(p => p.ProductId == id);

            if (exists)
            {
                cart.RemoveAll(p => p.ProductId == id);
                SaveCart(cart);
            }

            return Json(BuildCartResponse(cart, removed: exists));
        }

        public IActionResult Checkout()
        {
            var cart = GetCart();
            return View(cart);
        }

        [HttpPost]
        public IActionResult PlaceOrder(string address, string paymentMethod)
        {
            var cart = GetCart();
            if (!cart.Any())
            {
                return RedirectToAction("Index");
            }

            int? userId = null;
            var customerName = "Khách vãng lai";

            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdClaim, out var parsedUserId))
                {
                    userId = parsedUserId;
                }

                customerName = User.FindFirstValue(ClaimTypes.Name)
                    ?? User.FindFirstValue(ClaimTypes.Email)
                    ?? "Khách hàng";
            }

            var order = new Order
            {
                UserID = userId,
                CustomerName = customerName,
                OrderDate = DateTime.Now,
                ShippingAddress = address,
                PaymentMethod = paymentMethod,
                Status = OrderStatus.Pending,
                TotalAmount = cart.Sum(x => x.Price * x.Quantity)
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

            HttpContext.Session.Remove(CARTKEY);

            return RedirectToAction("Success");
        }

        public IActionResult Success()
        {
            return View();
        }

        private List<CartItem> GetCart()
        {
            var session = HttpContext.Session.GetString(CARTKEY);

            if (!string.IsNullOrEmpty(session))
            {
                return JsonSerializer.Deserialize<List<CartItem>>(session)
                       ?? new List<CartItem>();
            }

            return new List<CartItem>();
        }

        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetString(
                CARTKEY,
                JsonSerializer.Serialize(cart)
            );
        }

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


