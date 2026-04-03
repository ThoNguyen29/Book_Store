using System.Security.Claims;
using System.Text.Json;
using Book_Store.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Book_Store.Pages
{
    public class CartPageModel : PageModel
    {
        private const string CartKeyPrefix = "cart_user_";

        public List<CartItem> CartItems { get; private set; } = new();

        public IActionResult OnGet()
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                var loginUrl = Url.Action("Login", "Account", new { returnUrl = ResolveReturnUrl() }) ?? "/Account/Login";
                return Redirect(loginUrl);
            }

            CartItems = GetCart(userId);
            return Page();
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

        private bool TryGetCurrentUserId(out int userId)
        {
            userId = 0;
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out userId);
        }

        private string ResolveReturnUrl() => $"{Request.Path}{Request.QueryString}";

        private static string GetCartKey(int userId) => $"{CartKeyPrefix}{userId}";
    }
}
