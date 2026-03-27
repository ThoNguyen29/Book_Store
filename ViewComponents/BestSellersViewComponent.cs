using Book_Store.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Book_Store.ViewComponents
{
    public class BestSellersViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _db;

        public BestSellersViewComponent(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync(int take = 10)
        {
            var topIds = await _db.OrderDetails
                .AsNoTracking()
                .GroupBy(od => od.BookID)
                .OrderByDescending(g => g.Sum(x => x.Quantity))
                .Take(take)
                .Select(g => g.Key)
                .ToListAsync();

            if (topIds.Count == 0)
                return View(new List<Book>());

            var books = await _db.Books
                .AsNoTracking()
                .Include(b => b.BookImages)
                .Include(b => b.Category)
                .Where(b => b.IsActive && topIds.Contains(b.BookID))
                .ToListAsync();

            var ordered = topIds
                .Select(id => books.FirstOrDefault(b => b.BookID == id))
                .Where(b => b != null)
                .Select(b => b!)
                .ToList();

            return View(ordered);
        }
    }
}
