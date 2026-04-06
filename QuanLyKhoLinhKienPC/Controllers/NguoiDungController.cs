using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyKhoLinhKienPC.Models;
using QuanLyKhoLinhKienPC.Helpers;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace QuanLyKhoLinhKienPC.Controllers
{
    [Authorize(Roles = "Quản trị viên,Admin")]
    public class NguoiDungController : Controller
    {
        private readonly QuanLyKhoLinhKienPCContext _context;

        public NguoiDungController(QuanLyKhoLinhKienPCContext context)
        {
            _context = context;
        }

        // 1. DANH SÁCH (Chỉ hiện cái chưa xóa)
        // GET: NguoiDung
        public async Task<IActionResult> Index(string searchString, int? MaVaiTro)
        {
            var dsNguoiDung = _context.NguoiDung
                .Include(n => n.MaVaiTroNavigation)
                .Where(d => d.IsDeleted == false);

            if (!string.IsNullOrEmpty(searchString))
            {
                dsNguoiDung = dsNguoiDung.Where(d => d.HoTen.Contains(searchString) || d.TenDangNhap.Contains(searchString));
            }

            if (MaVaiTro.HasValue)
            {
                dsNguoiDung = dsNguoiDung.Where(d => d.MaVaiTro == MaVaiTro);
            }

            ViewData["MaVaiTro"] = new SelectList(_context.VaiTro.Where(v => !v.IsDeleted), "MaVaiTro", "TenVaiTro", MaVaiTro);
            ViewData["CurrentFilter"] = searchString;

            return View(await dsNguoiDung.ToListAsync());
        }

        // 2. CHI TIẾT
        // GET: NguoiDung/Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            var nguoiDung = await _context.NguoiDung
                .Include(n => n.MaVaiTroNavigation)
                .FirstOrDefaultAsync(m => m.MaNguoiDung == id);

            if (nguoiDung == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            return View(nguoiDung);
        }

        // 3. TẠO MỚI
        // GET: NguoiDung/Create
        public IActionResult Create()
        {
            ViewData["MaVaiTro"] = new SelectList(_context.VaiTro.Where(v => !v.IsDeleted), "MaVaiTro", "TenVaiTro");
            return View();
        }

        // POST: NguoiDung/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaNguoiDung,TenDangNhap,MatKhau,HoTen,Email,MaVaiTro,IsDeleted")] NguoiDung nguoiDung)
        {
            // Bỏ qua Validate mặc định cho khóa ngoại điều hướng và các cột ảo (Xác nhận mật khẩu) không dùng ở form này
            ModelState.Remove("MaVaiTroNavigation");
            ModelState.Remove("PhieuNhap");
            ModelState.Remove("PhieuXuat");
            ModelState.Remove("XacNhanMatKhau");

            // Kiểm tra trùng Tên Đăng Nhập
            if (_context.NguoiDung.Any(n => n.TenDangNhap == nguoiDung.TenDangNhap))
            {
                ModelState.AddModelError("TenDangNhap", "Tên đăng nhập này đã tồn tại trong hệ thống!");
            }

            if (ModelState.IsValid)
            {
                // Mã hóa mật khẩu trước khi lưu
                nguoiDung.MatKhau = SecurityHelper.HashPassword(nguoiDung.MatKhau);

                _context.Add(nguoiDung);
                await _context.SaveChangesAsync();
                await ActivityLogger.LogAsync(_context, int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1"), "Thêm mới", "Người Dùng", $"Thêm nhân viên: {nguoiDung.HoTen}");
                TempData["Success"] = "Thêm mới Người Dùng thành công!";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Vui lòng kiểm tra lại thông tin nhập!";
            ViewData["MaVaiTro"] = new SelectList(_context.VaiTro.Where(v => !v.IsDeleted), "MaVaiTro", "TenVaiTro", nguoiDung.MaVaiTro);
            return View(nguoiDung);
        }



        // 5. XÓA MỀM (Chuyển vào thùng rác)
        // GET: NguoiDung/Delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            var nguoiDung = await _context.NguoiDung
                .Include(n => n.MaVaiTroNavigation)
                .FirstOrDefaultAsync(m => m.MaNguoiDung == id);

            if (nguoiDung == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            return View(nguoiDung);
        }

        // POST: NguoiDung/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var nguoiDung = await _context.NguoiDung.FindAsync(id);
            if (nguoiDung != null)
            {
                // Logic chống đạn: Kiểm tra xem có đang tự xóa chính mình không
                var currentUserId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (currentUserId == id.ToString())
                {
                    TempData["Error"] = "Thao tác bị chặn: Bạn không thể tự khóa/xóa tài khoản của chính mình được!";
                    return RedirectToAction(nameof(Index));
                }

                // Logic xóa mềm
                nguoiDung.IsDeleted = true;
                _context.Update(nguoiDung);
                await _context.SaveChangesAsync();
                await ActivityLogger.LogAsync(_context, int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1"), "Xóa", "Người Dùng", $"Khóa nhân viên: {nguoiDung.HoTen}");
                TempData["Success"] = "Đã chuyển Người Dùng vào thùng rác.";
            }
            return RedirectToAction(nameof(Index));
        }

        // 6. THÙNG RÁC (Hiện danh sách đã xóa)
        // GET: NguoiDung/Trash
        public async Task<IActionResult> Trash(string searchString, int? MaVaiTro)
        {
            var dsNguoiDung = _context.NguoiDung
                .Include(n => n.MaVaiTroNavigation)
                .Where(d => d.IsDeleted == true);

            if (!string.IsNullOrEmpty(searchString))
            {
                dsNguoiDung = dsNguoiDung.Where(d => d.HoTen.Contains(searchString) || d.TenDangNhap.Contains(searchString));
            }

            if (MaVaiTro.HasValue)
            {
                dsNguoiDung = dsNguoiDung.Where(d => d.MaVaiTro == MaVaiTro);
            }

            ViewData["MaVaiTro"] = new SelectList(_context.VaiTro.Where(v => !v.IsDeleted), "MaVaiTro", "TenVaiTro", MaVaiTro);
            ViewData["CurrentFilter"] = searchString;

            return View(await dsNguoiDung.ToListAsync());
        }

        // 7. KHÔI PHỤC (Hồi sinh từ thùng rác)
        // POST: NguoiDung/Restore
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var nguoiDung = await _context.NguoiDung.FindAsync(id);
            if (nguoiDung == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Trash));
            }
            nguoiDung.IsDeleted = false;
            _context.Update(nguoiDung);
            await _context.SaveChangesAsync();
            await ActivityLogger.LogAsync(_context, int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1"), "Khôi phục", "Người Dùng", $"Mở khóa nhân viên: {nguoiDung.HoTen}");
            TempData["Success"] = "Khôi phục Người Dùng thành công.";
            return RedirectToAction(nameof(Trash));
        }

        private bool NguoiDungExists(int id)
        {
            return _context.NguoiDung.Any(e => e.MaNguoiDung == id);
        }
    }
}
