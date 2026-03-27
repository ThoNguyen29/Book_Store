using Book_Store.ViewModel.Products;
using Microsoft.AspNetCore.Mvc;

namespace Book_Store.ViewComponents
{
    public class ProductListFilterViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(ProductListFilterViewModel model)
        {
            return View(model);
        }
    }
}
