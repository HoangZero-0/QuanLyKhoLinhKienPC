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
            // Bỏ qua Validate mặc định cho khóa ngoại điều hướng
            ModelState.Remove("MaVaiTroNavigation");
            ModelState.Remove("PhieuNhap");
            ModelState.Remove("PhieuXuat");

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
                TempData["Success"] = "Thêm mới người dùng thành công!";
                return RedirectToAction(nameof(Index));
            }
            
            TempData["Error"] = "Vui lòng kiểm tra lại thông tin nhập!";
            ViewData["MaVaiTro"] = new SelectList(_context.VaiTro.Where(v => !v.IsDeleted), "MaVaiTro", "TenVaiTro", nguoiDung.MaVaiTro);
            return View(nguoiDung);
        }

        // 4. CHỈNH SỬA
        // GET: NguoiDung/Edit
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            var nguoiDung = await _context.NguoiDung.FindAsync(id);
            if (nguoiDung == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            ViewData["MaVaiTro"] = new SelectList(_context.VaiTro.Where(v => !v.IsDeleted), "MaVaiTro", "TenVaiTro", nguoiDung.MaVaiTro);
            return View(nguoiDung);
        }

        // POST: NguoiDung/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaNguoiDung,TenDangNhap,MatKhau,HoTen,Email,MaVaiTro,IsDeleted")] NguoiDung nguoiDung)
        {
            if (id != nguoiDung.MaNguoiDung)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            ModelState.Remove("MaVaiTroNavigation");
            ModelState.Remove("PhieuNhap");
            ModelState.Remove("PhieuXuat");

            if (ModelState.IsValid)
            {
                try
                {
                    // Giữ lại mật khẩu cũ nếu Admin không đổi mật khẩu
                    var oldNguoiDung = await _context.NguoiDung.AsNoTracking().FirstOrDefaultAsync(n => n.MaNguoiDung == id);
                    
                    if (string.IsNullOrEmpty(nguoiDung.MatKhau))
                    {
                        nguoiDung.MatKhau = oldNguoiDung.MatKhau;
                    }
                    else
                    {
                        // Nếu có nhập mật khẩu mới, băm mật khẩu mới đó
                        nguoiDung.MatKhau = SecurityHelper.HashPassword(nguoiDung.MatKhau);
                    }

                    _context.Update(nguoiDung);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật người dùng thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NguoiDungExists(nguoiDung.MaNguoiDung))
                    {
                        TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            
            ViewData["Error"] = "Vui lòng kiểm tra lại thông tin nhập!";
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
                // Kiểm tra xem có đang tự xóa chính mình không (có thể để dành cho Auth sau)
                // Logic xóa mềm
                nguoiDung.IsDeleted = true;
                _context.Update(nguoiDung);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã chuyển người dùng vào thùng rác.";
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
            TempData["Success"] = "Khôi phục người dùng thành công.";
            return RedirectToAction(nameof(Trash));
        }

        // 8. XÓA VĨNH VIỄN (Chỉ xóa được khi không có ràng buộc)
        // POST: NguoiDung/DeleteForce
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteForce(int id)
        {
            var nguoiDung = await _context.NguoiDung.FindAsync(id);
            if (nguoiDung == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Trash));
            }

            try
            {
                _context.NguoiDung.Remove(nguoiDung);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa vĩnh viễn người dùng.";
                return RedirectToAction(nameof(Trash));
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Không thể xóa vĩnh viễn người dùng này vì họ đã thực hiện các giao dịch nhập/xuất kho!";
                return RedirectToAction(nameof(Trash));
            }
        }

        // 9. DỌN SẠCH THÙNG RÁC (Phiên bản thông minh: Xóa được bao nhiêu thì xóa)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmptyTrash()
        {
            // Lấy tất cả danh sách trong thùng rác
            var racList = await _context.NguoiDung.Where(v => v.IsDeleted == true).ToListAsync();

            if (!racList.Any())
            {
                return RedirectToAction(nameof(Trash));
            }

            int daXoa = 0;
            int biLoi = 0;

            foreach (var item in racList)
            {
                try
                {
                    // Cố gắng xóa từng cái
                    _context.NguoiDung.Remove(item);
                    await _context.SaveChangesAsync(); // Lưu ngay lập tức
                    daXoa++;
                }
                catch (DbUpdateException)
                {
                    // Nếu lỗi (do ràng buộc khóa ngoại), bỏ qua và đếm lỗi
                    biLoi++;

                    // QUAN TRỌNG: Phải reset trạng thái của item bị lỗi về "Chưa thay đổi"
                    // Nếu không, EF Core sẽ vẫn nhớ lệnh xóa này và gây lỗi cho item tiếp theo
                    _context.Entry(item).State = EntityState.Unchanged;
                }
            }

            // Thông báo kết quả cho người dùng
            if (daXoa > 0 && biLoi == 0)
            {
                TempData["Success"] = $"Đã dọn sạch thùng rác ({daXoa} người dùng).";
            }
            else if (daXoa > 0 && biLoi > 0)
            {
                TempData["Warning"] = $"Đã xóa vĩnh viễn {daXoa} người dùng. Còn lại {biLoi} người dùng không thể xóa do đang sử dụng.";
            }
            else if (daXoa == 0 && biLoi > 0)
            {
                TempData["Error"] = "Không thể xóa người dùng nào vì tất cả đều đã tham gia hệ thống!";
            }

            return RedirectToAction(nameof(Trash));
        }

        private bool NguoiDungExists(int id)
        {
            return _context.NguoiDung.Any(e => e.MaNguoiDung == id);
        }
    }
}
