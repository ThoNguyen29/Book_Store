using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
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

    public IActionResult Index()
    {
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