using Book_Store.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Book_Store.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class AuthorsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AuthorsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /Authors
        public async Task<IActionResult> Index(string? q)
        {
            var query = _db.Authors.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(a => a.Name.Contains(q));
            }

            var authors = await query
                .OrderBy(a => a.Name)
                .ToListAsync();

            return View(authors);
        }

        // GET: /Authors/Create
        public IActionResult Create()
        {
            return View(new Author());
        }

        // POST: /Authors/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Author model)
        {
            if (!ModelState.IsValid) return View(model);

            // chặn trùng tên (basic)
            var exists = await _db.Authors.AnyAsync(a => a.Name == model.Name);
            if (exists)
            {
                ModelState.AddModelError(nameof(model.Name), "Tác giả đã tồn tại.");
                return View(model);
            }

            _db.Authors.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /Authors/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var author = await _db.Authors.FindAsync(id);
            if (author == null) return NotFound();

            return View(author);
        }

        // POST: /Authors/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Author model)
        {
            if (id != model.AuthorID) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var author = await _db.Authors.FindAsync(id);
            if (author == null) return NotFound();

            // chặn trùng tên với bản ghi khác
            var exists = await _db.Authors.AnyAsync(a => a.AuthorID != id && a.Name == model.Name);
            if (exists)
            {
                ModelState.AddModelError(nameof(model.Name), "Tác giả đã tồn tại.");
                return View(model);
            }

            author.Name = model.Name;
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // POST: /Authors/Delete (xóa 1, dùng confirm bar)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var author = await _db.Authors.FindAsync(id);
            if (author == null) return NotFound();

            // Chặn xóa nếu tác giả đang gắn với sách (BookAuthors)
            var hasLinks = await _db.BookAuthors.AnyAsync(ba => ba.AuthorID == id);
            if (hasLinks)
            {
                TempData["Error"] = "Không thể xóa vì tác giả đang được gắn với sách.";
                return RedirectToAction(nameof(Index));
            }

            _db.Authors.Remove(author);
            await _db.SaveChangesAsync();

            // reseed về 1 nếu bảng rỗng
            if (!await _db.Authors.AnyAsync())
            {
                await _db.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('dbo.Authors', RESEED, 0);");
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Authors/BulkDelete (xóa nhiều)
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

            // Chặn xóa nếu có liên kết BookAuthors
            var blocked = await _db.BookAuthors
                .Where(ba => idList.Contains(ba.AuthorID))
                .Select(ba => ba.AuthorID)
                .Distinct()
                .ToListAsync();

            if (blocked.Count > 0)
            {
                TempData["Error"] = "Không thể xóa một số tác giả vì đang được gắn với sách.";
                return RedirectToAction(nameof(Index));
            }

            var authors = await _db.Authors.Where(a => idList.Contains(a.AuthorID)).ToListAsync();
            _db.Authors.RemoveRange(authors);
            await _db.SaveChangesAsync();

            if (!await _db.Authors.AnyAsync())
            {
                await _db.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('dbo.Authors', RESEED, 0);");
            }

            return RedirectToAction(nameof(Index));
        }
    }
}