using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        // ===========================
        // HIỂN THỊ GIỎ HÀNG
        // ===========================
        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }

        // ===========================
        // THÊM SẢN PHẨM (AJAX)
        // ===========================
        public IActionResult AddToCart(int id)
        {
            var cart = GetCart();

            var book = _context.Books
                .Include(b => b.BookImages)
                .FirstOrDefault(b => b.BookID == id);

            if (book == null)
                return Json(new { count = cart.Sum(x => x.Quantity) });

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
                    Quantity = 1
                });
            }
            else
            {
                item.Quantity++;
            }

            SaveCart(cart);

            return Json(new { count = cart.Sum(x => x.Quantity) });
        }

        // ===========================
        // CẬP NHẬT SỐ LƯỢNG
        // ===========================
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

        // ===========================
        // XOÁ SẢN PHẨM
        // ===========================
        public IActionResult Remove(int id)
        {
            var cart = GetCart();
            cart.RemoveAll(p => p.ProductId == id);
            SaveCart(cart);

            return RedirectToAction("Index");
        }

        // ===========================
        // CHECKOUT
        // ===========================
        public IActionResult Checkout()
        {
            var cart = GetCart();
            return View(cart);
        }

        [HttpPost]
[HttpPost]
public IActionResult PlaceOrder(string address, string paymentMethod)
{
    var cart = GetCart();
    if (!cart.Any())
        return RedirectToAction("Index");

    var order = new Order
    {
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

    HttpContext.Session.Remove("cart");

    return RedirectToAction("Success");
}
        // ===========================
        // SESSION HELPER
        // ===========================
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
        public IActionResult Success()
{
    return View();
}
    }
}