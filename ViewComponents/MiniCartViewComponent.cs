using System.Text.Json;
using Book_Store.Models;
using Book_Store.ViewModel.Cart;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Book_Store.ViewComponents
{
    public class MiniCartViewComponent : ViewComponent
    {
        private const string CartKeyPrefix = "cart_user_";

        public IViewComponentResult Invoke()
        {
            var cart = new List<CartItem>();

            var userIdClaim = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return View(new MiniCartViewModel());
            }

            var session = HttpContext.Session.GetString($"{CartKeyPrefix}{userId}");

            if (!string.IsNullOrEmpty(session))
            {
                cart = JsonSerializer.Deserialize<List<CartItem>>(session)
                       ?? new List<CartItem>();
            }

            var model = new MiniCartViewModel
            {
                Count = cart.Sum(x => x.Quantity),
                Total = cart.Sum(x => x.Price * x.Quantity)
            };

            return View(model);
        }
    }
}
