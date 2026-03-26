# 2.5. CONTROLLER VÀ VIEWS

## 2.5.1 ViewBag

### Khái niệm

ViewBag là một dynamic object cho phép truyền dữ liệu từ Controller đến View. Nó là một cách lỏng lẻo (loosely typed) để chia sẻ dữ liệu giữa Controller và View.

### Đặc điểm

- **Dynamic object**: ViewBag là một đối tượng động, không có kiểu dữ liệu cụ thể
- **Tạm thời**: Dữ liệu chỉ tồn tại trong một HTTP request
- **Dễ sử dụng**: Không cần khai báo trước, chỉ cần gán giá trị
- **Không type-safe**: KO có kiểm tra kiểu dữ liệu tại thời điểm biên dịch

### Cách sử dụng

**Trong Controller:**

```csharp
public IActionResult Index()
{
    ViewBag.Title = "Trang chủ";
    ViewBag.Author = "Tác giả dự án";
    ViewBag.BookCount = 150;
    return View();
}
```

**Trong View (Razor):**

```html
<h1>@ViewBag.Title</h1>
<p>Tác giả: @ViewBag.Author</p>
<p>Số lượng sách: @ViewBag.BookCount</p>
```

### Ưu điểm

- Đơn giản, nhanh chóng để truyền dữ liệu
- Không cần tạo ViewModel

### Nhược điểm

- Không an toàn về kiểu dữ liệu
- Khó debug và maintain
- KO có IntelliSense trong View

### So sánh với ViewData

ViewData và ViewBag hoạt động tương tự nhau, nhưng:

- **ViewData**: Sử dụng từ điển (Dictionary), truy cập bằng string key
- **ViewBag**: Dynamic wrapper xung quanh ViewData, truy cập bằng properties

```csharp
// ViewData
ViewData["Title"] = "Trang chủ";

// ViewBag (tương đương)
ViewBag.Title = "Trang chủ";
```

---

## 2.5.2 TempData

### Khái niệm

TempData là một cơ chế lưu trữ dữ liệu tạm thời giữa hai HTTP requests. Nó thường được sử dụng để chuyển dữ liệu sau khi Redirect.

### Đặc điểm

- **Tồn tại qua redirect**: Dữ liệu vẫn còn sau khi thực hiện RedirectToAction, RedirectToRoute
- **Lưu trữ tạm thời**: Dữ liệu bị xóa sau khi được đọc lần đầu tiên (nếu KO keep lại)
- **Dictionary-based**: Sử dụng key-value storage như ViewData
- **Session-dependent**: Phụ thuộc vào cấu hình Session

### Cách sử dụng

**Lưu trữ dữ liệu:**

```csharp
public IActionResult Create(Book book)
{
    // Thêm sách vào database
    db.Books.Add(book);
    db.SaveChanges();

    // Lưu message vào TempData
    TempData["Message"] = "Thêm sách thành công!";

    // Redirect đến trang danh sách
    return RedirectToAction("Index");
}
```

**Lấy dữ liệu:**

```csharp
public IActionResult Index()
{
    // TempData tự động bị xóa sau khi được đọc
    ViewBag.Message = TempData["Message"];
    return View();
}
```

**Trong View:**

```html
@if (ViewBag.Message != null) {
<div class="alert alert-success">@ViewBag.Message</div>
}
```

### Keep dữ liệu

Nếu muốn giữ lại dữ liệu TempData để sử dụng nhiều lần:

```csharp
// Giữ lại giá trị
TempData.Keep("Message");

// Hoặc giữ toàn bộ
TempData.Keep();

// Hoặc sử dụng Peek
var message = TempData.Peek("Message");
```

### Ứng dụng

- Hiển thị thông báo sau khi thực hiện thao tác (Create, Update, Delete)
- Chuyển dữ liệu thanh toán sau khi redirect
- Lưu thông báo lỗi sau khi redirect

---

## 2.5.3 Partial View

### Khái niệm

Partial View là một View tái sử dụng chứa một phần của giao diện người dùng. Nó giống như một component có thể được nhúng vào các View khác.

### Đặc điểm

- **Tái sử dụng**: Cùng một Partial View có thể sử dụng trong nhiều View khác
- **Modular**: Giúp chia nhỏ code, dễ bảo trì
- **Có model riêng**: Có thể truyền model riêng cho Partial View
- **Không layout**: Partial View không có layout mặc định

### Cách tạo Partial View

**Tên file**: Bắt đầu với dấu `_`

```
_BookCard.cshtml
_CartSummary.cshtml
_ReviewForm.cshtml
```

**Nội dung:**

```html
<!-- Views/Shared/_BookCard.cshtml -->
@model Book

<div class="book-card">
  <h3>@Model.Title</h3>
  <p>Tác giả: @Model.Author.Name</p>
  <p>Giá: @Model.Price.ToString("C")</p>
  <a href="/Books/Detail/@Model.Id" class="btn btn-primary">Chi tiết</a>
</div>
```

### Cách sử dụng Partial View

**Phương pháp 1: Html.PartialAsync**

```html
@await Html.PartialAsync("_BookCard", book)
```

**Phương pháp 2: Với danh sách**

```html
@foreach(var book in Model.Books) { @await Html.PartialAsync("_BookCard", book)
}
```

**Phương pháp 3: Partial Tag Helper**

```html
<partial name="_BookCard" model="book" />
```

### Truyền dữ liệu cho Partial View

**Sử dụng ViewData:**

```csharp
// Trong Controller
ViewData["CategoryName"] = "Công nghệ";

// Trong Partial View
<p>@ViewData["CategoryName"]</p>
```

**Sử dụng Model:**

```csharp
// Trong Controller
var bookViewModel = new BookViewModel
{
    Books = books,
    Category = category
};
return View(bookViewModel);

// Trong View
@await Html.PartialAsync("_BookCard", Model.Books)
```

### Ứng dụng

- Header, Footer, Navigation menu
- Product Card components
- Form sections
- Widget hiển thị
- Reusable UI components

---

## 2.5.4 Content-Encoding

### Khái niệm

Content-Encoding là kỹ thuật nén dữ liệu được gửi từ Server đến Client để giảm kích thước truyền tải và tăng tốc độ tải trang.

### Các loại Encoding phổ biến

**GZIP**: Phổ biến nhất, nén dữ liệu hiệu quả (60-80% giảm dung lượng)

```
Content-Encoding: gzip
```

**Deflate**: Nén kém hiệu quả hơn GZIP

```
Content-Encoding: deflate
```

**Brotli**: Nén tốt hơn GZIP nhưng yêu cầu hỗ trợ của browser

```
Content-Encoding: br
```

### Cách cấu hình trong ASP.NET Core

**Trong Program.cs:**

```csharp
var builder = WebApplication.CreateBuilder(args);

// Thêm compression services
builder.Services.AddResponseCompression(options =>
{
    options.Providers.Add<GzipCompressionProvider>();
    options.Providers.Add<BrotliCompressionProvider>();
});

var app = builder.Build();

// Sử dụng compression middleware
app.UseResponseCompression();

app.Run();
```

### HTTP Headers

**Request Header (từ Client):**

```
Accept-Encoding: gzip, deflate, br
```

**Response Header (từ Server):**

```
Content-Encoding: gzip
Content-Length: 5234
```

### Ưu điểm

- Giảm dung lượng dữ liệu truyền (60-80%)
- Tăng tốc độ tải trang
- Tiết kiệm băng thông
- Cải thiện experience người dùng

### Nhược điểm

- Tiêu tốn CPU để nén/giải nén
- Không hiệu quả với dữ liệu đã nén (ảnh, video, PDF)
- Yêu cầu browser hỗ trợ

### Best Practices

- Enable compression cho HTML, CSS, JavaScript, JSON
- Disable cho file ảnh, video, PDF (đã nén)
- Cấu hình một mức nén phù hợp (balance giữa tốc độ và dung lượng)
- Kiểm tra tương thích browser

---

---

# 2.6. TAG HELPER

## 2.6.1 Các cài đặt cần thiết để sử dụng Tag Helper

### Khái niệm

Tag Helper là tính năng của ASP.NET Core cho phép tạo các HTML tag tùy chỉnh hoặc mở rộng các HTML tag sẵn có. Nó giúp viết Razor code gần giống HTML hơn, tăng khả năng đọc hiểu code.

### Cài đặt

**1. Cài đặt trong \_ViewImports.cshtml**

File `_ViewImports.cshtml` cần chứa directive để import Tag Helpers:

```html
@using Book_Store @using Book_Store.Models @addTagHelper *,
Microsoft.AspNetCore.Mvc.TagHelpers @addTagHelper *,
Your.Namespace.For.CustomTagHelpers
```

- `@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers`: Import tất cả Tag Helpers built-in từ ASP.NET Core
- `@addTagHelper *, YourNamespace`: Import custom Tag Helpers từ assembly của bạn

**2. Cài đặt trong \_ViewStart.cshtml**

File này xác định layout mặc định cho View:

```html
@{ Layout = "_Layout"; }
```

**3. Referencing assembly**

Đảm bảo project có reference đến `Microsoft.AspNetCore.Mvc.TagHelpers`:

```xml
<!-- Book_Store.csproj -->
<ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
</ItemGroup>
```

### Chế độ tương thích

Bạn có thể chọn chế độ hoạt động của Tag Helpers:

```html
<!-- Opt-in từng Tag Helper -->
@addTagHelper Microsoft.AspNetCore.Mvc.TagHelpers.FormTagHelper,
Microsoft.AspNetCore.Mvc.TagHelpers

<!-- Opt-out Tag Helper cụ thể -->
@removeTagHelper Microsoft.AspNetCore.Mvc.TagHelpers.FormTagHelper,
Microsoft.AspNetCore.Mvc.TagHelpers

<!-- Disable prefix -->
@tagHelperPrefix vc:
```

---

## 2.6.2 Giới thiệu Tag Helper cơ bản

### Khái niệm

Tag Helper là class kế thừa từ `TagHelper` hoặc `TagHelperComponent`. Chúng xử lý các HTML tag và transformational attributes.

### Các Tag Helper built-in phổ biến

**1. Anchor Tag Helper**

```html
<!-- Thay vì -->
<a href="/home/about">About</a>

<!-- Sử dụng Tag Helper -->
<a asp-controller="Home" asp-action="About">About</a>

<!-- Với parameter -->
<a asp-controller="Books" asp-action="Detail" asp-route-id="5">Chi tiết</a>

<!-- Với area -->
<a asp-area="Admin" asp-controller="Dashboard" asp-action="Index">Admin</a>
```

**Ưu điểm**:

- Tự động generate URL chính xác
- Dễ refactor (đổi route không cần fix URL)
- Support parameter và area
- Highlight link hiện tại (active)

**2. Form Tag Helper**

```html
<!-- Create form -->
<form
  asp-controller="Books"
  asp-action="Create"
  method="post"
  enctype="multipart/form-data"
>
  <!-- form content -->
</form>

<!-- Edit form (self-referencing) -->
<form asp-action="Edit" method="post">
  <!-- form content -->
</form>
```

**Ưu điểm**:

- Tự động set action URL
- Hỗ trợ anti-forgery token
- Generate hidden input cho HTTP method override

**3. Input Tag Helper**

```html
<!-- Text input -->
<input asp-for="Title" class="form-control" />

<!-- Number input -->
<input asp-for="Price" type="number" step="0.01" />

<!-- Email input -->
<input asp-for="Email" type="email" />

<!-- Checkbox -->
<input asp-for="IsActive" type="checkbox" />
```

**Tự động:**

- Set `id`, `name` từ property
- Set `type` dựa trên kiểu dữ liệu
- Add validation attributes
- Preserve giá trị trong form

**4. Textarea Tag Helper**

```html
<textarea asp-for="Description" class="form-control" rows="5"></textarea>
```

**5. Select Tag Helper**

```html
<!-- Bind tới List -->
<select asp-for="CategoryId" asp-items="Model.Categories" class="form-control">
  <option value="">-- Chọn danh mục --</option>
</select>

<!-- SelectList trong Controller -->
var categories = new SelectList( await db.Categories.ToListAsync(), "Id", "Name"
); ViewData["Categories"] = categories;
```

**6. Label Tag Helper**

```html
<label asp-for="Title"></label>
<!-- Output: <label for="Title">Title</label> -->
```

**7. Validation Message Tag Helper**

```html
<span asp-validation-for="Title" class="text-danger"></span>
```

**8. Validation Summary Tag Helper**

```html
<div asp-validation-summary="All" class="text-danger"></div>
```

---

## 2.6.3 Áp dụng Tag Helper sẵn có

### Ví dụ thực tế trong Book Store

**1. Form tạo sách mới**

```html
@model Book

<form asp-action="Create" method="post" enctype="multipart/form-data">
    <div asp-validation-summary="All" class="text-danger"></div>

    <div class="form-group">
        <label asp-for="Title"></label>
        <input asp-for="Title" class="form-control" placeholder="Nhập tiêu đề sách" />
        <span asp-validation-for="Title" class="text-danger"></span>
    </div>

    <div class="form-group">
        <label asp-for="AuthorId"></label>
        <select asp-for="AuthorId" asp-items="ViewData["Authors"] as SelectList" class="form-control">
            <option value="">-- Chọn tác giả --</option>
        </select>
        <span asp-validation-for="AuthorId" class="text-danger"></span>
    </div>

    <div class="form-group">
        <label asp-for="Price"></label>
        <input asp-for="Price" type="number" step="0.01" class="form-control" />
        <span asp-validation-for="Price" class="text-danger"></span>
    </div>

    <div class="form-group">
        <label asp-for="Description"></label>
        <textarea asp-for="Description" class="form-control" rows="4"></textarea>
        <span asp-validation-for="Description" class="text-danger"></span>
    </div>

    <div class="form-group">
        <label asp-for="IsActive"></label>
        <input asp-for="IsActive" type="checkbox" />
    </div>

    <button type="submit" class="btn btn-primary">Thêm sách</button>
    <a asp-action="Index" class="btn btn-secondary">Hủy</a>
</form>
```

**2. Danh sách sách với link**

```html
@model IEnumerable<Book>
  <table class="table">
    <thead>
      <tr>
        <th>Tiêu đề</th>
        <th>Tác giả</th>
        <th>Giá</th>
        <th>Thao tác</th>
      </tr>
    </thead>
    <tbody>
      @foreach (var book in Model) {
      <tr>
        <td>@book.Title</td>
        <td>@book.Author.Name</td>
        <td>@book.Price.ToString("C")</td>
        <td>
          <a
            asp-action="Detail"
            asp-route-id="@book.Id"
            class="btn btn-info btn-sm"
            >Xem</a
          >
          <a
            asp-action="Edit"
            asp-route-id="@book.Id"
            class="btn btn-warning btn-sm"
            >Sửa</a
          >
          <a
            asp-action="Delete"
            asp-route-id="@book.Id"
            class="btn btn-danger btn-sm"
            >Xóa</a
          >
        </td>
      </tr>
      }
    </tbody>
  </table></Book
>
```

**3. Trang chi tiết sản phẩm**

```html
@model Book

<h1>@Model.Title</h1>

<div class="product-details">
  <p>
    <strong>Tác giả:</strong>
    <a
      asp-controller="Authors"
      asp-action="Detail"
      asp-route-id="@Model.AuthorId"
      >@Model.Author.Name</a
    >
  </p>
  <p><strong>Nhà xuất bản:</strong> @Model.Publisher.Name</p>
  <p><strong>Giá:</strong> @Model.Price.ToString("C")</p>
  <p><strong>Mô tả:</strong> @Model.Description</p>

  <form asp-controller="Cart" asp-action="Add" method="post">
    <input type="hidden" name="bookId" value="@Model.Id" />
    <input type="number" name="quantity" value="1" min="1" />
    <button type="submit" class="btn btn-primary">Thêm vào giỏ</button>
  </form>

  <div class="mt-3">
    <a asp-action="Index" class="btn btn-secondary">Quay lại</a>
    @if (User.IsInRole("Admin")) {
    <a asp-action="Edit" asp-route-id="@Model.Id" class="btn btn-warning"
      >Chỉnh sửa</a
    >
    }
  </div>
</div>
```

### So sánh: HTML cổ điển vs Tag Helper

| HTML cổ điển                                    | Tag Helper                                                                    | Lợi ích Tag Helper                 |
| ----------------------------------------------- | ----------------------------------------------------------------------------- | ---------------------------------- |
| `<a href="/Books/Detail/5">Chi tiết</a>`        | `<a asp-controller="Books" asp-action="Detail" asp-route-id="5">Chi tiết</a>` | Tự động generate URL, dễ refactor  |
| `<input type="text" id="Title" name="Title" />` | `<input asp-for="Title" />`                                                   | Tự động id, name, type, validation |
| `<select id="AuthorId" name="AuthorId">`        | `<select asp-for="AuthorId" asp-items="...">`                                 | Binding dữ liệu tự động            |
| `<form action="/Books/Create" method="post">`   | `<form asp-action="Create" method="post">`                                    | Anti-forgery token, tự động URL    |

### Best Practices sử dụng Tag Helper

1. **Luôn sử dụng `asp-for`** để bind property, tránh hard-code id/name
2. **Sử dụng `asp-validation-summary`** và `asp-validation-for`\*\* cho form validation
3. **Sử dụng `asp-controller`, `asp-action`** thay vì hard-code URL
4. **Import Tag Helper đúng cách** trong `_ViewImports.cshtml`
5. **Sử dụng `asp-items`** cho dropdown/select list
6. **Validate dữ liệu** trên cả client (HTML5) và server

---

## Tóm tắt

| Chủ đề               | Mục đích                            | Cách sử dụng                                      |
| -------------------- | ----------------------------------- | ------------------------------------------------- |
| **ViewBag**          | Truyền dữ liệu từ Controller → View | `ViewBag.PropertyName = value;`                   |
| **TempData**         | Truyền dữ liệu qua redirect         | `TempData["key"] = value;`                        |
| **Partial View**     | Tái sử dụng UI components           | `@await Html.PartialAsync("_PartialName", model)` |
| **Content-Encoding** | Nén dữ liệu truyền tải              | Cấu hình trong `Program.cs`                       |
| **Tag Helper**       | Render HTML với binding             | `<input asp-for="Property" />`                    |
