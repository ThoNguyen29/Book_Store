using Book_Store.Models;
using Book_Store.ViewModel.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Products()
        {
            return View();
        }

        public IActionResult Categories()
        {
            return View();
        }

        public async Task<IActionResult> UserManagement()
        {
            var users = await _context.Users
                .AsNoTracking()
                .OrderBy(u => u.FullName)
                .ThenBy(u => u.Email)
                .Select(u => new AdminUserItemViewModel
                {
                    UserId = u.ID,
                    FullName = u.FullName,
                    Email = u.Email,
                    Role = u.Role ?? "Customer",
                    OrderCount = u.Orders.Count,
                    LastOrderDate = u.Orders
                        .OrderByDescending(o => o.OrderDate)
                        .Select(o => (DateTime?)o.OrderDate)
                        .FirstOrDefault()
                })
                .ToListAsync();

            foreach (var user in users)
            {
                if (string.IsNullOrWhiteSpace(user.FullName))
                {
                    user.FullName = user.Email;
                }

                if (string.IsNullOrWhiteSpace(user.Role))
                {
                    user.Role = "Customer";
                }
            }

            var model = new AdminUserManagementViewModel
            {
                Users = users
            };

            return View(model);
        }

        public async Task<IActionResult> UserOrders(int id)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.ID == id);

            if (user == null)
            {
                return NotFound();
            }

            var userName = user.FullName ?? string.Empty;
            var userEmail = user.Email ?? string.Empty;

            var orders = await _context.Orders
                .AsNoTracking()
                .Where(o => o.UserID == id
                    || (o.UserID == null && (o.CustomerName == userName || o.CustomerName == userEmail)))
                .Include(o => o.OrderDetails)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new AdminOrderSummaryItemViewModel
                {
                    OrderId = o.OrderID,
                    CustomerName = o.CustomerName,
                    OrderDate = o.OrderDate,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status,
                    ItemCount = o.OrderDetails.Sum(od => od.Quantity)
                })
                .ToListAsync();

            var fallbackName = !string.IsNullOrWhiteSpace(user.FullName)
                ? user.FullName
                : user.Email ?? string.Empty;

            foreach (var order in orders)
            {
                if (string.IsNullOrWhiteSpace(order.CustomerName))
                {
                    order.CustomerName = fallbackName;
                }
            }

            var model = new AdminUserOrdersViewModel
            {
                UserId = user.ID,
                FullName = fallbackName,
                Email = user.Email ?? string.Empty,
                Orders = orders
            };

            return View(model);
        }

        public async Task<IActionResult> OrderDetails(int id)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Book)
                .FirstOrDefaultAsync(o => o.OrderID == id);

            if (order == null)
            {
                return NotFound();
            }

            var fallbackName = !string.IsNullOrWhiteSpace(order.User?.FullName)
                ? order.User.FullName
                : order.User?.Email ?? "Khach vang lai";

            var model = new AdminOrderDetailsViewModel
            {
                UserId = order.UserID,
                OrderId = order.OrderID,
                OrderDate = order.OrderDate,
                CustomerName = string.IsNullOrWhiteSpace(order.CustomerName) ? fallbackName : order.CustomerName ?? fallbackName,
                CustomerEmail = order.User?.Email ?? "-",
                ShippingAddress = order.ShippingAddress,
                PaymentMethod = order.PaymentMethod,
                Status = order.Status,
                NextStatus = GetNextStatus(order.Status),
                CanCancel = CanCancel(order.Status),
                TotalAmount = order.TotalAmount,
                Items = order.OrderDetails
                    .Select(od => new AdminOrderProductItemViewModel
                    {
                        BookId = od.BookID,
                        BookTitle = od.Book != null ? od.Book.Title : $"Sach #{od.BookID}",
                        Quantity = od.Quantity,
                        UnitPrice = od.Price,
                        LineTotal = od.Price * od.Quantity
                    })
                    .ToList()
            };

            return View(model);
        }

        public async Task<IActionResult> OrderList()
        {
            var orders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            foreach (var order in orders)
            {
                if (string.IsNullOrWhiteSpace(order.CustomerName))
                {
                    order.CustomerName = !string.IsNullOrWhiteSpace(order.User?.FullName)
                        ? order.User.FullName
                        : order.User?.Email ?? "Khach vang lai";
                }
            }

            return View(orders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AdvanceStatus(int id)
        {
            var order = _context.Orders.Find(id);
            if (order == null)
            {
                return NotFound();
            }

            var nextStatus = GetNextStatus(order.Status);
            if (!nextStatus.HasValue)
            {
                TempData["OrderStatusError"] = "Don hang khong the chuyen tiep trang thai.";
                return RedirectToAction(nameof(OrderDetails), new { id });
            }

            order.Status = nextStatus.Value;
            _context.SaveChanges();

            TempData["OrderStatusSuccess"] = $"Da cap nhat trang thai sang {GetStatusLabel(order.Status)}.";
            return RedirectToAction(nameof(OrderDetails), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelOrder(int id)
        {
            var order = _context.Orders.Find(id);
            if (order == null)
            {
                return NotFound();
            }

            if (!CanCancel(order.Status))
            {
                TempData["OrderStatusError"] = "Don hang nay khong the huy.";
                return RedirectToAction(nameof(OrderDetails), new { id });
            }

            order.Status = OrderStatus.Cancelled;
            _context.SaveChanges();

            TempData["OrderStatusSuccess"] = "Da huy don hang.";
            return RedirectToAction(nameof(OrderDetails), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateStatus(int id, OrderStatus status)
        {
            var order = _context.Orders.Find(id);

            if (order == null)
            {
                return RedirectToAction(nameof(OrderList));
            }

            if (!IsValidTransition(order.Status, status))
            {
                TempData["OrderStatusError"] = "Chi duoc cap nhat theo thu tu trang thai. Neu can, hay dung nut Huy don.";
                return RedirectToAction(nameof(OrderList));
            }

            order.Status = status;
            _context.SaveChanges();

            TempData["OrderStatusSuccess"] = $"Da cap nhat trang thai sang {GetStatusLabel(status)}.";
            return RedirectToAction(nameof(OrderList));
        }

        private static OrderStatus? GetNextStatus(OrderStatus currentStatus)
        {
            return currentStatus switch
            {
                OrderStatus.Pending => OrderStatus.Confirmed,
                OrderStatus.Confirmed => OrderStatus.Shipping,
                OrderStatus.Shipping => OrderStatus.Completed,
                _ => null
            };
        }

        private static bool CanCancel(OrderStatus currentStatus)
        {
            return currentStatus != OrderStatus.Completed && currentStatus != OrderStatus.Cancelled;
        }

        private static bool IsValidTransition(OrderStatus currentStatus, OrderStatus targetStatus)
        {
            if (targetStatus == OrderStatus.Cancelled)
            {
                return CanCancel(currentStatus);
            }

            var nextStatus = GetNextStatus(currentStatus);
            return nextStatus.HasValue && nextStatus.Value == targetStatus;
        }

        private static string GetStatusLabel(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => "Cho xac nhan",
                OrderStatus.Confirmed => "Da xac nhan",
                OrderStatus.Shipping => "Dang giao",
                OrderStatus.Completed => "Hoan thanh",
                OrderStatus.Cancelled => "Da huy",
                _ => status.ToString()
            };
        }
    }
}
