using Book_Store.Models;
using Book_Store.ViewModel.users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Book_Store.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public RegisterModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public RegisterViewModel Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var isExist = _context.Users.Any(u => u.Email == Input.Email);

        if (isExist)
        {
            ModelState.AddModelError(string.Empty, "Email này đã được đăng ký.");
            return Page();
        }

        var user = new User
        {
            Email = Input.Email,
            FullName = Input.FullName,
            Username = Input.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Input.Password),
            Role = "Customer",
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đăng ký thành công. Vui lòng đăng nhập.";
        return RedirectToPage("/Account/Login");
    }
}
