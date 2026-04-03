using System.Diagnostics;
using Book_Store.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
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

    [HttpGet]
    public async Task<IActionResult> SearchSuggest(string? term, int? categoryId)
    {
        if (string.IsNullOrWhiteSpace(term))
            return Json(Array.Empty<object>());

        term = term.Trim();
        if (term.Length < 2)
            return Json(Array.Empty<object>());

        var query = _db.Books
            .AsNoTracking()
            .Where(b => b.IsActive && b.Title.Contains(term));

        if (categoryId.HasValue)
            query = query.Where(b => b.CategoryID == categoryId.Value);

        var result = await query
            .OrderBy(b => b.Title)
            .Select(b => new { id = b.BookID, title = b.Title })
            .Take(8)
            .ToListAsync();

        return Json(result);
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
    public IActionResult Login(string? returnUrl = null)
    {
        return LocalRedirect(BuildAccountPageUrl("/Account/Login", returnUrl));
    }

    [HttpPost]
    [ActionName("Login")]
    public IActionResult LoginPost(string? returnUrl = null)
    {
        return LocalRedirect(BuildAccountPageUrl("/Account/Login", returnUrl));
    }

    [HttpGet]
    public IActionResult Register()
    {
        return LocalRedirect("/Account/Register");
    }

    [HttpPost]
    [ActionName("Register")]
    public IActionResult RegisterPost()
    {
        return LocalRedirect("/Account/Register");
    }

    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private static string BuildAccountPageUrl(string pagePath, string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return pagePath;
        }

        return $"{pagePath}?returnUrl={Uri.EscapeDataString(returnUrl)}";
    }
}
