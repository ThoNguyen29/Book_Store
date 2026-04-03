using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Book_Store.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json;

[Authorize]
public class OrderController : Controller
{
    private readonly ApplicationDbContext _context;
    private const string CartKeyPrefix = "cart_user_";

    public OrderController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return RedirectToAction(nameof(History));
    }

    public IActionResult Details(int id)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var order = _context.Orders
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Book)
            .FirstOrDefault(o => o.OrderID == id && o.UserID == userId);

        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }

    public IActionResult History()
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var orders = _context.Orders
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Book)
            .Where(o => o.UserID == userId)
            .OrderByDescending(o => o.OrderDate)
            .ToList();

        return View(orders);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Reorder(int id)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var order = _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderDetails)
            .FirstOrDefault(o => o.OrderID == id && o.UserID == userId);

        if (order == null)
        {
            return NotFound();
        }

        if (!order.OrderDetails.Any())
        {
            return RedirectToAction("History");
        }

        var orderedBookIds = order.OrderDetails
            .Select(od => od.BookID)
            .Distinct()
            .ToList();

        var books = _context.Books
            .AsNoTracking()
            .Include(b => b.BookImages)
            .Where(b => orderedBookIds.Contains(b.BookID))
            .ToDictionary(b => b.BookID);

        var cart = GetUserCart(userId);

        foreach (var detail in order.OrderDetails)
        {
            if (!books.TryGetValue(detail.BookID, out var book))
            {
                continue;
            }

            if (book.Stock <= 0)
            {
                continue;
            }

            var qtyToAdd = Math.Min(detail.Quantity, book.Stock);
            if (qtyToAdd <= 0)
            {
                continue;
            }

            var existing = cart.FirstOrDefault(c => c.ProductId == detail.BookID);
            if (existing == null)
            {
                cart.Add(new CartItem
                {
                    ProductId = book.BookID,
                    ProductName = book.Title,
                    Price = detail.Price,
                    Quantity = qtyToAdd,
                    Image = book.BookImages?
                        .FirstOrDefault(i => i.IsPrimary)?.ImagePath
                        ?? "/images/no-image.png"
                });
            }
            else
            {
                existing.Quantity = Math.Min(existing.Quantity + qtyToAdd, book.Stock);
            }
        }

        SaveUserCart(userId, cart);
        return RedirectToAction("Index", "Cart");
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        userId = 0;
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out userId);
    }

    private List<CartItem> GetUserCart(int userId)
    {
        var sessionValue = HttpContext.Session.GetString(GetCartKey(userId));
        if (string.IsNullOrEmpty(sessionValue))
        {
            return new List<CartItem>();
        }

        return JsonSerializer.Deserialize<List<CartItem>>(sessionValue) ?? new List<CartItem>();
    }

    private void SaveUserCart(int userId, List<CartItem> cart)
    {
        HttpContext.Session.SetString(
            GetCartKey(userId),
            JsonSerializer.Serialize(cart)
        );
    }

    private static string GetCartKey(int userId) => $"{CartKeyPrefix}{userId}";
}
