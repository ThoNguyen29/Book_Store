using Book_Store.Models;
using Microsoft.EntityFrameworkCore;
using Book_Store.ViewModel.users;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// 1. Thêm các dịch vụ vào Container
builder.Services.AddControllersWithViews();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 2. Kết nối Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. Cấu hình Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.Cookie.Name = "BookStore.Auth";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

var app = builder.Build();

// 4. Cấu hình Pipeline xử lý HTTP (Thứ tự rất quan trọng)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Sử dụng Session trước Authentication
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// 5. Khởi tạo dữ liệu Admin mẫu nếu chưa có
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (!db.Users.Any(u => u.Email == "admin@bookstore.local"))
    {
        db.Users.Add(new User
        {
            Email = "admin@bookstore.local",
            FullName = "System Administrator",
            Username = "admin", 
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"), 
            Role = "Admin",
            Address = "System",
            IsActive = true,
            CreatedAt = DateTime.Now
        });
        db.SaveChanges();
    }
}

// 6. Cấu hình Route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();