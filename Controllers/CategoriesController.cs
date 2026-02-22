using Book_Store.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Book_Store.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CategoriesController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /Categories
        public async Task<IActionResult> Index()
        {
            var categories = await _db.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(categories);
        }

        // GET: /Categories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category model)
        {
            if (!ModelState.IsValid) return View(model);

            // Slug có thể để null; nếu muốn tự sinh slug thì cần thêm hàm xử lý riêng
            _db.Categories.Add(model);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: /Categories/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _db.Categories.FindAsync(id);
            if (category == null) return NotFound();

            return View(category);
        }

        // POST: /Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category model)
        {
            if (id != model.CategoryID) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var category = await _db.Categories.FindAsync(id);
            if (category == null) return NotFound();

            category.Name = model.Name;
            category.Slug = model.Slug;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /Categories/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _db.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null) return NotFound();
            return View(category);
        }

        // POST: /Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _db.Categories.FindAsync(id);
            if (category == null) return NotFound();

            // Nếu category đang có Books, xóa sẽ lỗi FK (vì Book.CategoryID là FK)
            // Cách xử lý: chặn xóa nếu có sách
            var hasBooks = await _db.Books.AnyAsync(b => b.CategoryID == id);
            if (hasBooks)
            {
                ModelState.AddModelError("", "Không thể xóa vì Category đang có Books.");
                return View(category);
            }

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();
            var isEmpty = !await _db.Categories.AnyAsync();
            if (isEmpty)
            {
                await _db.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('dbo.Categories', RESEED, 0);");
            }
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDelete(string ids)
        {
            if (string.IsNullOrWhiteSpace(ids))
                return RedirectToAction(nameof(Index));

            var idList = ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => int.TryParse(s, out var n) ? n : (int?)null)
                            .Where(n => n.HasValue)
                            .Select(n => n!.Value)
                            .Distinct()
                            .ToList();

            if (idList.Count == 0)
                return RedirectToAction(nameof(Index));

            // Chặn xóa nếu category có book (tránh lỗi FK)
            var blocked = await _db.Books.Where(b => b.CategoryID != null && idList.Contains(b.CategoryID.Value))
                                        .Select(b => b.CategoryID!.Value)
                                        .Distinct()
                                        .ToListAsync();

            if (blocked.Count > 0)
            {
                TempData["Error"] = "Không thể xóa một số danh mục vì đang có sách thuộc danh mục đó.";
                return RedirectToAction(nameof(Index));
            }

            var categories = await _db.Categories.Where(c => idList.Contains(c.CategoryID)).ToListAsync();
            _db.Categories.RemoveRange(categories);
            await _db.SaveChangesAsync();
            var isEmpty = !await _db.Categories.AnyAsync();
            if (isEmpty)
            {
                await _db.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('dbo.Categories', RESEED, 0);");
            }
            return RedirectToAction(nameof(Index));
        }
        
    }
}