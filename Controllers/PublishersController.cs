using Book_Store.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Book_Store.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class PublishersController : Controller
    {
        private readonly ApplicationDbContext _db;

        public PublishersController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /Publishers
        public async Task<IActionResult> Index(string? q)
        {
            var query = _db.Publishers.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(p => p.Name.Contains(q));
            }

            var publishers = await query
                .OrderBy(p => p.Name)
                .ToListAsync();

            return View(publishers);
        }

        // GET: /Publishers/Create
        public IActionResult Create()
        {
            return View(new Publisher());
        }

        // POST: /Publishers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Publisher model)
        {
            if (!ModelState.IsValid) return View(model);

            var exists = await _db.Publishers.AnyAsync(p => p.Name == model.Name);
            if (exists)
            {
                ModelState.AddModelError(nameof(model.Name), "Nhà xuất bản đã tồn tại.");
                return View(model);
            }

            _db.Publishers.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /Publishers/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var publisher = await _db.Publishers.FindAsync(id);
            if (publisher == null) return NotFound();

            return View(publisher);
        }

        // POST: /Publishers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Publisher model)
        {
            if (id != model.PublisherID) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var publisher = await _db.Publishers.FindAsync(id);
            if (publisher == null) return NotFound();

            var exists = await _db.Publishers.AnyAsync(p => p.PublisherID != id && p.Name == model.Name);
            if (exists)
            {
                ModelState.AddModelError(nameof(model.Name), "Nhà xuất bản đã tồn tại.");
                return View(model);
            }

            publisher.Name = model.Name;
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // POST: /Publishers/Delete (xóa 1, dùng confirm bar)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var publisher = await _db.Publishers.FindAsync(id);
            if (publisher == null) return NotFound();

            // Chặn xóa nếu nhà xuất bản đang gắn với sách
            var hasBooks = await _db.Books.AnyAsync(b => b.PublisherID == id);
            if (hasBooks)
            {
                TempData["Error"] = "Không thể xóa vì nhà xuất bản đang được gắn với sách.";
                return RedirectToAction(nameof(Index));
            }

            _db.Publishers.Remove(publisher);
            await _db.SaveChangesAsync();

            if (!await _db.Publishers.AnyAsync())
            {
                await _db.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('dbo.Publishers', RESEED, 0);");
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Publishers/BulkDelete (xóa nhiều)
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

            var blocked = await _db.Books
                .Where(b => b.PublisherID != null && idList.Contains(b.PublisherID.Value))
                .Select(b => b.PublisherID!.Value)
                .Distinct()
                .ToListAsync();

            if (blocked.Count > 0)
            {
                TempData["Error"] = "Không thể xóa một số nhà xuất bản vì đang được gắn với sách.";
                return RedirectToAction(nameof(Index));
            }

            var publishers = await _db.Publishers.Where(p => idList.Contains(p.PublisherID)).ToListAsync();
            _db.Publishers.RemoveRange(publishers);
            await _db.SaveChangesAsync();

            if (!await _db.Publishers.AnyAsync())
            {
                await _db.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('dbo.Publishers', RESEED, 0);");
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

