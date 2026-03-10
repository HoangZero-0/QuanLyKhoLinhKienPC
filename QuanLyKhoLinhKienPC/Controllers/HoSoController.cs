using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhoLinhKienPC.Models;
using QuanLyKhoLinhKienPC.Helpers;

namespace QuanLyKhoLinhKienPC.Controllers
{
    [Authorize]
    public class HoSoController : Controller
    {
        private readonly QuanLyKhoLinhKienPCContext _context;

        public HoSoController(QuanLyKhoLinhKienPCContext context)
        {
            _context = context;
        }

        // GET: HoSo/Index (Xem thông tin)
        public async Task<IActionResult> Index()
        {
            // Lấy ID người dùng hiện tại từ Claims
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var userIdClaim = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int maNguoiDung))
            {
                return RedirectToAction("Login", "Auth");
            }

            var nguoiDung = await _context.NguoiDung
                .Include(n => n.MaVaiTroNavigation)
                .FirstOrDefaultAsync(m => m.MaNguoiDung == maNguoiDung);

            if (nguoiDung == null)
            {
                return NotFound();
            }

            return View(nguoiDung);
        }

        // POST: HoSo/CapNhatThongTin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index([Bind("HoTen,Email")] NguoiDung model)
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var userIdClaim = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int maNguoiDung))
            {
                return RedirectToAction("Login", "Auth");
            }

            var nguoiDung = await _context.NguoiDung.FindAsync(maNguoiDung);
            
            if (nguoiDung == null)
            {
                return NotFound();
            }

            // Chỉ cho phép cập nhật Họ Tên và Email
            nguoiDung.HoTen = model.HoTen;
            nguoiDung.Email = model.Email;

            try
            {
                _context.Update(nguoiDung);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật thông tin cá nhân thành công!";
                
                // Lưu ý: Cập nhật CSDL thì Cookie tạm thời chưa cập nhật HoTen ngay lập tức (phải đăng nhập lại), 
                // nhưng về mặt dữ liệu thì đã đúng. 
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Có lỗi xảy ra khi lưu dữ liệu.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: HoSo/DoiMatKhau
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoiMatKhau(string matKhauCu, string matKhauMoi, string xacNhanMatKhauMoi)
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var userIdClaim = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int maNguoiDung))
            {
                return RedirectToAction("Login", "Auth");
            }

            // Validation cơ bản
            if (string.IsNullOrEmpty(matKhauCu) || string.IsNullOrEmpty(matKhauMoi) || string.IsNullOrEmpty(xacNhanMatKhauMoi))
            {
                TempData["ErrorDoiMK"] = "Vui lòng nhập đầy đủ các trường mật khẩu.";
                return RedirectToAction(nameof(Index));
            }

            if (matKhauMoi != xacNhanMatKhauMoi)
            {
                TempData["ErrorDoiMK"] = "Mật khẩu mới và xác nhận mật khẩu không khớp nhau.";
                return RedirectToAction(nameof(Index));
            }

            var nguoiDung = await _context.NguoiDung.FindAsync(maNguoiDung);
            
            if (nguoiDung == null)
            {
                return NotFound();
            }

            // Kiểm tra Mật Khẩu Cũng
            if (!SecurityHelper.VerifyPassword(matKhauCu, nguoiDung.MatKhau))
            {
                TempData["ErrorDoiMK"] = "Mật khẩu hiện tại không chính xác.";
                return RedirectToAction(nameof(Index));
            }

            // Cập nhật mk mới (Băm mật khẩu)
            nguoiDung.MatKhau = SecurityHelper.HashPassword(matKhauMoi);

            try
            {
                _context.Update(nguoiDung);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đổi mật khẩu thành công!";
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Có lỗi xảy ra khi cập nhật mật khẩu.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
