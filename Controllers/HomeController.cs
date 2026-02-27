using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Book_Store.Models;

namespace Book_Store.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _db;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        // Fetch latest books for the homepage
        var latestBooks = await _db.Books
            .AsNoTracking()
            .Include(b => b.BookImages)
            .Include(b => b.Category)
            .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
            .Where(b => b.IsActive)
            .OrderByDescending(b => b.CreatedAt)
            .Take(10)
            .ToListAsync();

        ViewBag.LatestBooks = latestBooks;
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }
    public IActionResult About()
    {
        return View();
    }

    public async Task<IActionResult> ProductList(
        string? q,
        int? categoryId,
        decimal? minPrice,
        decimal? maxPrice,
        int? publisherId,
        int? authorId,
        string? stockStatus,
        string? sort,
        int page = 1)
    {
        var query = _db.Books
            .AsNoTracking()
            .Include(b => b.BookImages)
            .Include(b => b.Category)
            .Include(b => b.Publisher)
            .Include(b => b.BookAuthors)
                .ThenInclude(ba => ba.Author)
            .Where(b => b.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(b => b.Title.Contains(q));
        }

        if (categoryId.HasValue)
            query = query.Where(b => b.CategoryID == categoryId.Value);

        if (minPrice.HasValue)
            query = query.Where(b => b.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(b => b.Price <= maxPrice.Value);

        if (publisherId.HasValue)
            query = query.Where(b => b.PublisherID == publisherId.Value);

        if (authorId.HasValue)
            query = query.Where(b => b.BookAuthors.Any(ba => ba.AuthorID == authorId.Value));

        if (!string.IsNullOrWhiteSpace(stockStatus) && stockStatus != "all")
        {
            switch (stockStatus)
            {
                case "in":
                    query = query.Where(b => b.Stock > 0);
                    break;
                case "out":
                    query = query.Where(b => b.Stock == 0);
                    break;
                case "low":
                    query = query.Where(b => b.Stock > 0 && b.Stock <= 5);
                    break;
            }
        }

        query = sort switch
        {
            "price_asc" => query.OrderBy(b => b.Price).ThenBy(b => b.Title),
            "price_desc" => query.OrderByDescending(b => b.Price).ThenBy(b => b.Title),
            "title_asc" => query.OrderBy(b => b.Title),
            "title_desc" => query.OrderByDescending(b => b.Title),
            _ => query.OrderByDescending(b => b.CreatedAt).ThenBy(b => b.Title)
        };

        const int pageSize = 12;
        if (page < 1) page = 1;

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        if (totalPages == 0) totalPages = 1;
        if (page > totalPages) page = totalPages;

        var books = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Danh mục để lọc
        var categories = await _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
        var publishers = await _db.Publishers.AsNoTracking().OrderBy(p => p.Name).ToListAsync();
        var authors = await _db.Authors.AsNoTracking().OrderBy(a => a.Name).ToListAsync();
        ViewBag.Categories = categories;
        ViewBag.Publishers = publishers;
        ViewBag.Authors = authors;
        ViewBag.CurrentQ = q;
        ViewBag.CurrentCategoryId = categoryId;
        ViewBag.CurrentMinPrice = minPrice;
        ViewBag.CurrentMaxPrice = maxPrice;
        ViewBag.CurrentPublisherId = publisherId;
        ViewBag.CurrentAuthorId = authorId;
        ViewBag.CurrentStockStatus = stockStatus;
        ViewBag.CurrentSort = sort;
        ViewBag.Page = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalItems = totalItems;

        return View(books);
    }

    public async Task<IActionResult> ProductDetail(int id)
    {
        var book = await _db.Books
            .AsNoTracking()
            .Include(b => b.BookImages.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder))
            .Include(b => b.Category)
            .Include(b => b.Publisher)
            .Include(b => b.BookAuthors)
                .ThenInclude(ba => ba.Author)
            .FirstOrDefaultAsync(b => b.BookID == id && b.IsActive);

        if (book == null)
            return RedirectToAction(nameof(ProductList));

        // Similar products: same category, exclude current book, max 10
        var relatedBooks = new List<Book>();
        if (book.CategoryID.HasValue)
        {
            relatedBooks = await _db.Books
                .AsNoTracking()
                .Include(b => b.BookImages)
                .Include(b => b.Category)
                .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
                .Where(b => b.IsActive && b.CategoryID == book.CategoryID && b.BookID != book.BookID)
                .OrderByDescending(b => b.CreatedAt)
                .Take(10)
                .ToListAsync();
        }

        ViewBag.RelatedBooks = relatedBooks;
        return View(book);
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(object _)
    {
        // TODO: implement login logic
        return View();
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View("~/Views/Account/Register.cshtml");
    }

    [HttpPost]
    public IActionResult Register(string firstName, string lastName, string email, string password, string confirmPassword)
    {
        // 1. Kiểm tra validation thủ công
        if (string.IsNullOrWhiteSpace(firstName)) ModelState.AddModelError("firstName", "Tên là bắt buộc");
        if (string.IsNullOrWhiteSpace(lastName)) ModelState.AddModelError("lastName", "Họ là bắt buộc");
        if (string.IsNullOrWhiteSpace(email)) ModelState.AddModelError("email", "Email là bắt buộc");
        else if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            ModelState.AddModelError("email", "Email không hợp lệ");

        if (string.IsNullOrWhiteSpace(password)) ModelState.AddModelError("password", "Mật khẩu là bắt buộc");
        else if (password.Length < 6) ModelState.AddModelError("password", "Mật khẩu phải từ 6 ký tự trở lên");

        if (password != confirmPassword) ModelState.AddModelError("confirmPassword", "Mật khẩu xác nhận không khớp");

        if (!ModelState.IsValid)
        {
            return View("~/Views/Account/Register.cshtml");
        }

        // 2. Tạo đối tượng User mới
        var user = new User
        {
            Email = email.Trim(),
            Username = email.Trim(), // Thêm Username vì model User yêu cầu
            PasswordHash = HashPassword(password), // Đã sửa tên gọi hàm cho đúng
            FullName = $"{lastName} {firstName}".Trim(),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _db.Users!.Add(user);
        _db.SaveChanges();

        TempData["SuccessMessage"] = "Đăng ký thành công. Vui lòng đăng nhập.";
        return RedirectToAction("Login");
    }

    // Hàm băm mật khẩu
    private static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
