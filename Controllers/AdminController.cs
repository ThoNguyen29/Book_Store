using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Book_Store.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Book_Store.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==============================
        // DASHBOARD
        // ==============================
        public IActionResult Index()
        {
            return View();
        }

        // ==============================
        // DANH SÁCH SẢN PHẨM
        // ==============================
        public IActionResult Products()
        {
            return View();
        }

        // ==============================
        // DANH MỤC
        // ==============================
        public IActionResult Categories()
        {
            return View();
        }

        // ==============================
        // QUẢN LÝ NGƯỜI DÙNG
        // ==============================
        public IActionResult UserManagement()
        {
            return View();
        }

        // ==============================
        // KHO HÀNG
        // ==============================
        public IActionResult Inventory()
        {
            return View();
        }

        // ==============================
        // DANH SÁCH ĐƠN HÀNG
        // ==============================
        public async Task<IActionResult> OrderList()
        {
            var orders = await _context.Orders
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // ==============================
        // CẬP NHẬT TRẠNG THÁI ĐƠN HÀNG
        // ==============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateStatus(int id, OrderStatus status)
{
    var order = _context.Orders.Find(id);

    if (order != null)
    {
        order.Status = status;
        _context.SaveChanges();
    }

    return RedirectToAction("OrderList");
}
    }
}