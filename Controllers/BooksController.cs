using Book_Store.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace Book_Store.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class BooksController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpClientFactory _httpClientFactory;

        public BooksController(ApplicationDbContext db, IWebHostEnvironment env, IHttpClientFactory httpClientFactory)
        {
            _db = db;
            _env = env;
            _httpClientFactory = httpClientFactory;
        }

        // =========================
        // INDEX (HIỂN THỊ)
        // =========================
        public async Task<IActionResult> Index(string? q, int? categoryId, int? publisherId)
        {
            var query = _db.Books
                .AsNoTracking()
                .Include(b => b.Category)
                .Include(b => b.Publisher)
                .Include(b => b.BookImages)
                .Where(b => b.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(b => b.Title.Contains(q));
            }

            if (categoryId.HasValue) query = query.Where(b => b.CategoryID == categoryId.Value);
            if (publisherId.HasValue) query = query.Where(b => b.PublisherID == publisherId.Value);

            var books = await query
                .OrderByDescending(b => b.CreatedAt)
                .ThenBy(b => b.Title)
                .ToListAsync();

            await LoadLookups(categoryId, publisherId);
            return View(books);
        }

        // =========================
        // HIDDEN (ĐÃ ẨN)
        // =========================
        public async Task<IActionResult> Hidden(string? q, int? categoryId, int? publisherId)
        {
            var query = _db.Books
                .AsNoTracking()
                .Include(b => b.Category)
                .Include(b => b.Publisher)
                .Include(b => b.BookImages)
                .Where(b => !b.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(b => b.Title.Contains(q));
            }

            if (categoryId.HasValue) query = query.Where(b => b.CategoryID == categoryId.Value);
            if (publisherId.HasValue) query = query.Where(b => b.PublisherID == publisherId.Value);

            var books = await query
                .OrderByDescending(b => b.CreatedAt)
                .ThenBy(b => b.Title)
                .ToListAsync();

            await LoadLookups(categoryId, publisherId);
            return View(books);
        }

        // =========================
        // HIDE / RESTORE
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Hide(int id)
        {
            var book = await _db.Books.FindAsync(id);
            if (book == null) return NotFound();

            book.IsActive = false;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var book = await _db.Books.FindAsync(id);
            if (book == null) return NotFound();

            book.IsActive = true;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Hidden));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkHide(string ids)
        {
            var idList = ParseIds(ids);
            if (idList.Count == 0) return RedirectToAction(nameof(Index));

            var books = await _db.Books.Where(b => idList.Contains(b.BookID)).ToListAsync();
            foreach (var b in books) b.IsActive = false;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkRestore(string ids)
        {
            var idList = ParseIds(ids);
            if (idList.Count == 0) return RedirectToAction(nameof(Hidden));

            var books = await _db.Books.Where(b => idList.Contains(b.BookID)).ToListAsync();
            foreach (var b in books) b.IsActive = true;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Hidden));
        }
                // ====== GET Create ======
        public async Task<IActionResult> Create()
        {
            await LoadLookups();
            ViewBag.StaticImages = GetStaticImages();
            return View(new Book { IsActive = true, CreatedAt = DateTime.Now });
        }

        // ====== POST Create ======
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Book model,
            List<IFormFile>? uploadFiles,
            string? imageUrls,
            string? urlMode,            // "keep" | "download"
            List<string>? staticImages, // "/images/books/xxx.jpg"
            string? primaryChoice       // "url:0" | "static:0" | "auto"
        )
        {
            await LoadLookups(model.CategoryID, model.PublisherID);
            ViewBag.StaticImages = GetStaticImages();

            if (!ModelState.IsValid) return View(model);

            model.CreatedAt ??= DateTime.Now;

            _db.Books.Add(model);
            await _db.SaveChangesAsync(); // lấy BookID

            var mode = string.IsNullOrWhiteSpace(urlMode) ? "keep" : urlMode.Trim();

            // sort bắt đầu từ 0
            var images = await BuildImagesAsync(
                bookId: model.BookID,
                uploadFiles: uploadFiles,
                rawUrls: imageUrls,
                urlMode: mode,
                staticImages: staticImages,
                startSortOrder: 0
            );

            ApplyPrimaryChoice(images, primaryChoice);
            EnsurePrimary(images);

            if (images.Count > 0)
            {
                _db.BookImages.AddRange(images);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
                // ====== GET Edit ======
        public async Task<IActionResult> Edit(int id)
        {
            var book = await _db.Books
                .Include(b => b.BookImages)
                .FirstOrDefaultAsync(b => b.BookID == id);

            if (book == null) return NotFound();

            await LoadLookups(book.CategoryID, book.PublisherID);
            ViewBag.StaticImages = GetStaticImages();
            return View(book);
        }

        // ====== POST Edit ======
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Book model,
            List<IFormFile>? uploadFiles,
            string? imageUrls,
            string? urlMode,
            List<string>? staticImages,
            int? primaryImageId,
            List<int>? deleteImageIds
        )
        {
            if (id != model.BookID) return BadRequest();

            var book = await _db.Books
                .Include(b => b.BookImages)
                .FirstOrDefaultAsync(b => b.BookID == id);

            if (book == null) return NotFound();

            await LoadLookups(model.CategoryID, model.PublisherID);
            ViewBag.StaticImages = GetStaticImages();

            if (!ModelState.IsValid) return View(book);

            // update fields + trạng thái
            book.Title = model.Title;
            book.Price = model.Price;
            book.Stock = model.Stock;
            book.Description = model.Description;
            book.CategoryID = model.CategoryID;
            book.PublisherID = model.PublisherID;
            book.IsActive = model.IsActive;

            // 1) Xóa ảnh
            if (deleteImageIds != null && deleteImageIds.Count > 0)
            {
                var toDelete = book.BookImages.Where(x => deleteImageIds.Contains(x.BookImageID)).ToList();
                _db.BookImages.RemoveRange(toDelete);
            }

            // sort bắt đầu từ max+1
            var currentMaxSort = book.BookImages.Count == 0 ? -1 : book.BookImages.Max(x => x.SortOrder);
            var startSort = currentMaxSort + 1;

            var mode = string.IsNullOrWhiteSpace(urlMode) ? "keep" : urlMode.Trim();

            // 2) Thêm ảnh mới
            var newImages = await BuildImagesAsync(
                bookId: book.BookID,
                uploadFiles: uploadFiles,
                rawUrls: imageUrls,
                urlMode: mode,
                staticImages: staticImages,
                startSortOrder: startSort
            );

            if (newImages.Count > 0)
                _db.BookImages.AddRange(newImages);

            await _db.SaveChangesAsync();

            // 3) Set ảnh bìa
            var allImages = await _db.BookImages
                .Where(x => x.BookID == book.BookID)
                .OrderBy(x => x.SortOrder)
                .ToListAsync();

            if (primaryImageId.HasValue)
            {
                foreach (var img in allImages)
                    img.IsPrimary = (img.BookImageID == primaryImageId.Value);
            }

            EnsurePrimary(allImages);

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
                private async Task<List<BookImage>> BuildImagesAsync(
            int bookId,
            List<IFormFile>? uploadFiles,
            string? rawUrls,
            string urlMode,
            List<string>? staticImages,
            int startSortOrder
        )
        {
            var result = new List<BookImage>();
            var sort = startSortOrder;

            // A) Upload từ máy -> /uploads/books/
            if (uploadFiles != null && uploadFiles.Count > 0)
            {
                foreach (var f in uploadFiles)
                {
                    if (f == null || f.Length <= 0) continue;
                    if (!IsAllowedImage(f.ContentType, f.FileName)) continue;

                    var relPath = await SaveUploadAsync(f);
                    result.Add(new BookImage
                    {
                        BookID = bookId,
                        ImagePath = relPath,
                        IsPrimary = false,
                        SortOrder = sort++,
                        SourceType = "upload"
                    });
                }
            }

            // B) URL -> keep / download
            var urls = ParseUrls(rawUrls);
            if (urls.Count > 0)
            {
                foreach (var u in urls)
                {
                    if (!Uri.IsWellFormedUriString(u, UriKind.Absolute)) continue;

                    if (string.Equals(urlMode, "download", StringComparison.OrdinalIgnoreCase))
                    {
                        var relPath = await DownloadToUploadsAsync(u);
                        if (relPath != null)
                        {
                            result.Add(new BookImage
                            {
                                BookID = bookId,
                                ImagePath = relPath,
                                IsPrimary = false,
                                SortOrder = sort++,
                                SourceType = "download"
                            });
                        }
                    }
                    else
                    {
                        result.Add(new BookImage
                        {
                            BookID = bookId,
                            ImagePath = u,
                            IsPrimary = false,
                            SortOrder = sort++,
                            SourceType = "url"
                        });
                    }
                }
            }

            // C) Static image -> /images/books/
            if (staticImages != null && staticImages.Count > 0)
            {
                foreach (var p in staticImages.Distinct())
                {
                    if (string.IsNullOrWhiteSpace(p)) continue;
                    if (!p.StartsWith("/images/books/", StringComparison.OrdinalIgnoreCase)) continue;

                    result.Add(new BookImage
                    {
                        BookID = bookId,
                        ImagePath = p,
                        IsPrimary = false,
                        SortOrder = sort++,
                        SourceType = "static"
                    });
                }
            }

            return result;
        }

        private static void ApplyPrimaryChoice(List<BookImage> images, string? primaryChoice)
        {
            if (images.Count == 0) return;
            if (string.IsNullOrWhiteSpace(primaryChoice)) return;
            if (primaryChoice == "auto") return;

            var parts = primaryChoice.Split(':', 2);
            if (parts.Length != 2) return;

            var type = parts[0];
            if (!int.TryParse(parts[1], out var idx)) return;

            var candidates = images
                .Where(x => string.Equals(x.SourceType, type, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (idx < 0 || idx >= candidates.Count) return;

            foreach (var img in images) img.IsPrimary = false;
            candidates[idx].IsPrimary = true;
        }

        private static void EnsurePrimary(List<BookImage> images)
        {
            if (images.Count == 0) return;
            if (images.Any(x => x.IsPrimary)) return;
            images.OrderBy(x => x.SortOrder).First().IsPrimary = true;
        }

        private async Task<string> SaveUploadAsync(IFormFile file)
        {
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "books");
            Directory.CreateDirectory(uploadsDir);

            var ext = Path.GetExtension(file.FileName);
            var safeExt = string.IsNullOrWhiteSpace(ext) ? ".jpg" : ext.ToLowerInvariant();

            var name = $"{Guid.NewGuid():N}{safeExt}";
            var absPath = Path.Combine(uploadsDir, name);

            using var stream = System.IO.File.Create(absPath);
            await file.CopyToAsync(stream);

            return $"/uploads/books/{name}";
        }

        private async Task<string?> DownloadToUploadsAsync(string url)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                using var resp = await client.GetAsync(url);
                if (!resp.IsSuccessStatusCode) return null;

                var contentType = resp.Content.Headers.ContentType?.MediaType ?? "";
                if (!IsAllowedImage(contentType, url)) return null;

                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "books");
                Directory.CreateDirectory(uploadsDir);

                var ext = GuessExtension(contentType, url);
                var name = $"{Guid.NewGuid():N}{ext}";
                var absPath = Path.Combine(uploadsDir, name);

                var bytes = await resp.Content.ReadAsByteArrayAsync();
                await System.IO.File.WriteAllBytesAsync(absPath, bytes);

                return $"/uploads/books/{name}";
            }
            catch
            {
                return null;
            }
        }

        private static bool IsAllowedImage(string contentType, string nameOrUrl)
        {
            var ct = (contentType ?? "").ToLowerInvariant();
            if (ct.StartsWith("image/")) return true;

            var ext = Path.GetExtension(nameOrUrl)?.ToLowerInvariant();
            return ext is ".jpg" or ".jpeg" or ".png" or ".webp" or ".gif";
        }

        private static string GuessExtension(string contentType, string url)
        {
            var ct = (contentType ?? "").ToLowerInvariant();
            if (ct.Contains("png")) return ".png";
            if (ct.Contains("webp")) return ".webp";
            if (ct.Contains("gif")) return ".gif";
            if (ct.Contains("jpeg") || ct.Contains("jpg")) return ".jpg";

            var ext = Path.GetExtension(url);
            if (!string.IsNullOrWhiteSpace(ext)) return ext.ToLowerInvariant();
            return ".jpg";
        }

        private static List<string> ParseUrls(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return new List<string>();

            return raw.Split(new[] { '\r', '\n', ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                      .Select(x => WebUtility.HtmlDecode(x.Trim()))
                      .Where(x => !string.IsNullOrWhiteSpace(x))
                      .Distinct()
                      .ToList();
        }

        private List<string> GetStaticImages()
        {
            var dir = Path.Combine(_env.WebRootPath, "images", "books");
            if (!Directory.Exists(dir)) return new List<string>();

            var exts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

            return Directory.GetFiles(dir)
                .Where(f => exts.Contains(Path.GetExtension(f)))
                .Select(f => "/images/books/" + Path.GetFileName(f))
                .OrderBy(x => x)
                .ToList();
        }

        private async Task LoadLookups(int? selectedCategoryId = null, int? selectedPublisherId = null)
        {
            var categories = await _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
            var publishers = await _db.Publishers.AsNoTracking().OrderBy(p => p.Name).ToListAsync();

            ViewBag.CategoryID = new SelectList(categories, "CategoryID", "Name", selectedCategoryId);
            ViewBag.PublisherID = new SelectList(publishers, "PublisherID", "Name", selectedPublisherId);
        }

        private static List<int> ParseIds(string ids)
        {
            if (string.IsNullOrWhiteSpace(ids)) return new List<int>();

            return ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                      .Select(s => int.TryParse(s, out var n) ? n : (int?)null)
                      .Where(n => n.HasValue)
                      .Select(n => n!.Value)
                      .Distinct()
                      .ToList();
        }
    }
}