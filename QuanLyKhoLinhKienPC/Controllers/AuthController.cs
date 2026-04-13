using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using QuanLyKhoLinhKienPC.Models;
using QuanLyKhoLinhKienPC.Helpers;

namespace QuanLyKhoLinhKienPC.Controllers
{
    [AllowAnonymous]
    public class AuthController : Controller
    {
        private readonly QuanLyKhoLinhKienPCContext _context;

        public AuthController(QuanLyKhoLinhKienPCContext context)
        {
            _context = context;
        }

        // GET: Auth/Login
        [HttpGet]
        public IActionResult Login(string returnUrl = "/")
        {
            // Kiểm tra nếu đã đăng nhập rồi thì đá về trang chủ
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return Redirect(returnUrl);
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string tenDangNhap, string matKhau, string returnUrl = "/")
        {
            ViewData["ReturnUrl"] = returnUrl;

            // 1. Kiểm tra đầu vào rỗng
            if (string.IsNullOrEmpty(tenDangNhap) || string.IsNullOrEmpty(matKhau))
            {
                ViewData["Error"] = "Vui lòng nhập đầy đủ Tên đăng nhập và Mật khẩu.";
                return View();
            }

            // 2. Tìm User trong cơ sở dữ liệu (Lấy tất cả để báo lỗi tường minh)
            var user = await _context.NguoiDung
                .Include(u => u.MaVaiTroNavigation)
                .FirstOrDefaultAsync(u => u.TenDangNhap == tenDangNhap);

            if (user == null)
            {
                ViewData["Error"] = "Tên đăng nhập không tồn tại trong hệ thống.";
                return View();
            }

            // 3. Kiểm tra mật khẩu (Sử dụng hàm Verify từ SecurityHelper)
            if (!SecurityHelper.VerifyPassword(matKhau, user.MatKhau))
            {
                ViewData["Error"] = "Mật khẩu không chính xác.";
                return View();
            }

            // 4. KIỂM TRA TÀI KHOẢN KHÓA SAU KHI XÁC THỰC MẬT KHẨU ĐÚNG
            if (user.IsDeleted)
            {
                ViewData["Error"] = "Tài khoản này đã bị khóa. Vui lòng liên hệ quản trị viên!";
                return View();
            }

            // 5. Nếu hợp lệ, tạo hồ sơ Claims cho Cookie
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.MaNguoiDung.ToString()),
                new Claim(ClaimTypes.Name, user.TenDangNhap),
                new Claim("HoTen", user.HoTen ?? user.TenDangNhap),
                // Có thể lưu Vai Trò vào ClaimTypes.Role để sau này phân quyền: [Authorize(Roles = "Quản trị viên")]
                new Claim(ClaimTypes.Role, user.MaVaiTroNavigation?.TenVaiTro ?? "")
            };

            var claimsIdentity = new ClaimsIdentity(claims, "PCCookieAuth");
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true, // Ghi nhớ đăng nhập
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) // Lưu 7 ngày
            };

            // 6. Phát hành Cookie đăng nhập
            await HttpContext.SignInAsync(
                "PCCookieAuth",
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            TempData["Success"] = $"Chào mừng {user.HoTen} đã quay lại!";

            // Tránh lỗi Open Redirect Attack: Đảm bảo ReturnUrl thuộc về miền cục bộ
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        // POST: Auth/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Xóa Cookie đăng nhập
            await HttpContext.SignOutAsync("PCCookieAuth");

            return RedirectToAction("Login", "Auth");
        }

        // GET: Auth/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            // Trang hiển thị khi user không đủ quyền truy cập tính năng (Role)
            return View();
        }
    }
}
