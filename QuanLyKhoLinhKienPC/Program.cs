using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Thêm dòng này để kết nối Database
builder.Services.AddDbContext<QuanLyKhoLinhKienPC.Models.QuanLyKhoLinhKienPCContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    // Cấu hình yêu cầu đăng nhập trên TOÀN BỘ hệ thống (Global Filter)
    var policy = new AuthorizationPolicyBuilder()
                     .RequireAuthenticatedUser()
                     .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});

// Thêm cấu hình Cookie Authentication
builder.Services.AddAuthentication("PCCookieAuth")
    .AddCookie("PCCookieAuth", options =>
    {
        options.LoginPath = "/Auth/Login";       // Đường dẫn khi chưa đăng nhập mà truy cập trang bảo mật
        options.LogoutPath = "/Auth/Logout";     // Đường dẫn đăng xuất
        options.AccessDeniedPath = "/Auth/AccessDenied"; // Đường dẫn khi không có quyền (Role)
        options.ExpireTimeSpan = TimeSpan.FromDays(7);   // Cookie sống 7 ngày
        options.Cookie.HttpOnly = true;          // Bảo mật Cookie
        options.SlidingExpiration = true;        // Tự động gia hạn Cookie nếu user vẫn hoạt động
    });



var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Thêm UseAuthentication TRƯỚC UseAuthorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();