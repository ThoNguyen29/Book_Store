using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Book_Store.Controllers;

[Authorize(Policy = "AdminOnly")]
public class AdminController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public ViewResult Products()
    {
        return View();
    }
       public IActionResult Categories()
    {
        return View();
    }
       public IActionResult UserManagement()
    {
        return View();
    }
       public IActionResult OrderList()
    {
        return View();
    }
       public IActionResult Inventory()
    {
        return View();
    }
}
