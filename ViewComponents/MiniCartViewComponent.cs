using System.Text.Json;
using Book_Store.Models;
using Book_Store.ViewModel.Cart;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Book_Store.ViewComponents
{
    public class MiniCartViewComponent : ViewComponent
    {
        private const string CartKey = "cart";

        public IViewComponentResult Invoke()
        {
            var cart = new List<CartItem>();
            var session = HttpContext.Session.GetString(CartKey);

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
