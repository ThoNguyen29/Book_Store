using Book_Store.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Book_Store.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class BooksController : Controller
    {
        private readonly ApplicationDbContext _db;

        public BooksController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /Books
        public async Task<IActionResult> Index()
        {
            var books = await _db.Books
                .AsNoTracking()
                .Include(b => b.Category)
                .Include(b => b.Publisher)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(books);
        }

        // GET: /Books/Create
        public async Task<IActionResult> Create()
        {
            await LoadLookups();
            return View(new Book());
        }

        // POST: /Books/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Book model)
        {
            if (!ModelState.IsValid)
            {
                await LoadLookups();
                return View(model);
            }

            _db.Books.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /Books/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var book = await _db.Books.FindAsync(id);
            if (book == null) return NotFound();

            await LoadLookups();
            return View(book);
        }

        // POST: /Books/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Book model)
        {
            if (id != model.BookID) return BadRequest();

            if (!ModelState.IsValid)
            {
                await LoadLookups();
                return View(model);
            }

            var book = await _db.Books.FindAsync(id);
            if (book == null) return NotFound();

            book.Title = model.Title;
            book.Price = model.Price;
            book.Stock = model.Stock;
            book.Description = model.Description;
            book.CategoryID = model.CategoryID;
            book.PublisherID = model.PublisherID;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /Books/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var book = await _db.Books
                .AsNoTracking()
                .Include(b => b.Category)
                .Include(b => b.Publisher)
                .FirstOrDefaultAsync(b => b.BookID == id);

            if (book == null) return NotFound();
            return View(book);
        }

        // POST: /Books/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _db.Books.FindAsync(id);
            if (book == null) return NotFound();

            // Nếu book có OrderDetails hoặc BookAuthors, xóa có thể lỗi FK.
            // Cách tối thiểu: xóa các BookAuthors trước (nếu có).
            var bookAuthors = await _db.BookAuthors.Where(x => x.BookID == id).ToListAsync();
            if (bookAuthors.Count > 0) _db.BookAuthors.RemoveRange(bookAuthors);

            _db.Books.Remove(book);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadLookups()
        {
            var categories = await _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
            var publishers = await _db.Publishers.AsNoTracking().OrderBy(p => p.Name).ToListAsync();

            ViewBag.CategoryID = new SelectList(categories, "CategoryID", "Name");
            ViewBag.PublisherID = new SelectList(publishers, "PublisherID", "Name");
        }
    }
}