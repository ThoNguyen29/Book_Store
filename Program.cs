using Book_Store.Models;
using Microsoft.EntityFrameworkCore;
using Book_Store.ViewModel.users;
using Microsoft.AspNetCore.Authentication.Cookies;
using Book_Store.Models.Momo;
using Book_Store.Services.Momo;

var builder = WebApplication.CreateBuilder(args);

// Connect to Momo API
builder.Services.Configure<MomoOptionModel>(builder.Configuration.GetSection("MomoAPI"));
builder.Services.AddScoped<IMomoService, MomoService>();
builder.Services.AddLogging(config =>
{
    config.ClearProviders();
    config.AddConsole();
});

// Services
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpClient();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Authentication
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

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// Seed default admin if missing
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

app.MapBlazorHub();
app.MapRazorPages();
app.MapControllerRoute(
    name: "momo-checkout-callback",
    pattern: "Checkout/PaymentCallBack",
    defaults: new { controller = "Payment", action = "PaymentCallBack" });
app.MapControllerRoute(
    name: "momo-checkout-notify",
    pattern: "Checkout/MomoNotify",
    defaults: new { controller = "Payment", action = "MomoNotify" });
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
