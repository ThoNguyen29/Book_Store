using Book_Store.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Book_Store.ViewComponents
{
    public class LatestBooksViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _db;

        public LatestBooksViewComponent(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync(int take = 10)
        {
            var books = await _db.Books
                .AsNoTracking()
                .Include(b => b.BookImages)
                .Include(b => b.Category)
                .Where(b => b.IsActive)
                .OrderByDescending(b => b.CreatedAt)
                .Take(take)
                .ToListAsync();

            return View(books);
        }
    }
}
