using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Book_Store.Models;

public class OrderController : Controller
{
    private readonly ApplicationDbContext _context;

    public OrderController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var orders = _context.Orders.ToList();
        return View(orders);
    }

    public IActionResult Details(int id)
    {
        var order = _context.Orders
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Book)
            .FirstOrDefault(o => o.OrderID == id);

        return View(order);
    }
    public IActionResult History()
{
    var orders = _context.Orders
        .Include(o => o.OrderDetails)
        .ThenInclude(od => od.Book)
        .ToList();

    return View(orders);
}
}