using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Book_Store.Models;
using Book_Store.Models.Chat;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Book_Store.Controllers;

[Route("[controller]")]
public class ChatController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        ApplicationDbContext db,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ChatController> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("Ask")]
    public async Task<IActionResult> Ask([FromBody] ChatAskRequest request)
    {
        var userMessage = request.Message?.Trim();
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return BadRequest(new ChatAskResponse { Reply = "Ban hay nhap cau hoi truoc nhe." });
        }

        var apiKey =
            _configuration["Gemini:ApiKey"] ??
            Environment.GetEnvironmentVariable("GEMINI_API_KEY") ??
            Environment.GetEnvironmentVariable("Gemini__ApiKey");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return StatusCode(500, new ChatAskResponse
            {
                Reply = "Server chua duoc cau hinh Gemini API key. Ban them `Gemini:ApiKey` vao appsettings.Development.json giup minh nhe."
            });
        }

        var matchingBooks = await _db.Books
            .AsNoTracking()
            .Include(b => b.Category)
            .Include(b => b.Publisher)
            .Include(b => b.BookAuthors)
                .ThenInclude(ba => ba.Author)
            .Where(b => b.IsActive)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        var searchTerms = ExtractSearchTerms(userMessage);
        if (searchTerms.Count > 0)
        {
            matchingBooks = matchingBooks
                .Where(book => MatchesBook(book, searchTerms))
                .Take(5)
                .ToList();
        }
        else
        {
            matchingBooks = matchingBooks.Take(5).ToList();
        }

        var bookContext = matchingBooks.Count == 0
            ? "Khong tim thay sach nao khop ro rang trong kho du lieu hien tai."
            : string.Join("\n", matchingBooks.Select((book, index) =>
                $"{index + 1}. {book.Title} | Gia: {book.Price:#,##0} VND | Ton: {book.Stock} | The loai: {book.Category?.Name ?? "Chua ro"} | Tac gia: {string.Join(", ", book.BookAuthors.Select(x => x.Author.Name))}"));

        if (matchingBooks.Count > 0)
        {
            var directReply = "Minh tim thay cac sach phu hop trong cua hang:\n\n" +
                              string.Join("\n\n", matchingBooks.Select((book, index) =>
                                  $"{index + 1}. {book.Title}\n" +
                                  $"Gia: {book.Price:#,##0} VND\n" +
                                  $"The loai: {book.Category?.Name ?? "Chua ro"}\n" +
                                  $"Ton kho: {book.Stock}"));

            return Json(new ChatAskResponse { Reply = directReply });
        }

        var historyLines = request.History?
            .Where(x => !string.IsNullOrWhiteSpace(x.Text))
            .TakeLast(8)
            .Select(x => $"{(x.Role == "user" ? "Nguoi dung" : "Tro ly")}: {x.Text.Trim()}")
            .ToList() ?? new List<string>();

        var systemPrompt =
            "Ban la tro ly AI cho website nha sach Hai An.\n" +
            "Tra loi bang tieng Viet, gon gang, than thien, uu tien tu van mua sach.\n" +
            "Neu co du lieu sach lien quan ben duoi thi uu tien dua ra goi y tu kho sach cua cua hang.\n" +
            "Neu khong chac chan, hay noi ro la thong tin trong kho hien tai chua day du.\n" +
            "Khong tu nhan co chuc nang dat hang hoac thanh toan neu khong duoc hoi.\n\n" +
            "Sach lien quan trong kho:\n" +
            bookContext;

        var contents = new List<object>
        {
            new
            {
                role = "user",
                parts = new[]
                {
                    new
                    {
                        text = $"{systemPrompt}\n\nLich su hoi dap:\n{string.Join("\n", historyLines)}\n\nCau hoi moi nhat: {userMessage}"
                    }
                }
            }
        };

        var payload = new { contents };
        var client = _httpClientFactory.CreateClient();
        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

        try
        {
            using var response = await client.PostAsync(
                endpoint,
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

            var rawJson = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini error {StatusCode}: {Body}", response.StatusCode, rawJson);
                return StatusCode((int)response.StatusCode, new ChatAskResponse
                {
                    Reply = "Tro ly dang tam thoi gap loi khi ket noi Gemini. Ban thu lai sau it phut nhe."
                });
            }

            using var document = JsonDocument.Parse(rawJson);
            var reply = document.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return Json(new ChatAskResponse
            {
                Reply = string.IsNullOrWhiteSpace(reply)
                    ? "Minh chua tao duoc cau tra loi phu hop. Ban thu hoi ro hon mot chut nhe."
                    : reply.Trim()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected chat error");
            return StatusCode(500, new ChatAskResponse
            {
                Reply = "Da co loi phia server khi xu ly tro ly. Ban thu lai sau nhe."
            });
        }
    }

    private static bool MatchesBook(Book book, IReadOnlyCollection<string> searchTerms)
    {
        var searchableParts = new List<string?>
        {
            book.Title,
            book.Description,
            book.Category?.Name,
            book.Publisher?.Name
        };

        searchableParts.AddRange(book.BookAuthors.Select(x => x.Author.Name));
        var searchableText = NormalizeText(string.Join(" ", searchableParts.Where(x => !string.IsNullOrWhiteSpace(x))));

        return searchTerms.All(term => searchableText.Contains(term));
    }

    private static List<string> ExtractSearchTerms(string input)
    {
        var stopWords = new HashSet<string>
        {
            "sach", "cuon", "quyen", "tim", "kiem", "cho", "toi", "minh",
            "ve", "co", "khong", "khong?", "la", "nhung", "mot", "cac"
        };

        return Regex.Split(NormalizeText(input), @"\s+")
            .Where(term => term.Length >= 2 && !stopWords.Contains(term))
            .Distinct()
            .ToList();
    }

    private static string NormalizeText(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var normalized = input.Trim().ToLowerInvariant()
            .Replace('đ', 'd');

        var sb = new StringBuilder();
        foreach (var c in normalized.Normalize(NormalizationForm.FormD))
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
