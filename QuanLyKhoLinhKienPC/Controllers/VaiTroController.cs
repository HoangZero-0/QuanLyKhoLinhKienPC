using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyKhoLinhKienPC.Models;
using Microsoft.AspNetCore.Authorization;

namespace QuanLyKhoLinhKienPC.Controllers
{
    [Authorize(Roles = "Quản trị viên,Admin")]
    public class VaiTroController : Controller
    {
        private readonly QuanLyKhoLinhKienPCContext _context;

        public VaiTroController(QuanLyKhoLinhKienPCContext context)
        {
            _context = context;
        }

        // 1. DANH SÁCH (Chỉ hiện cái chưa xóa)
        // GET: VaiTro
        public async Task<IActionResult> Index(string searchString)
        {
            var dsVaiTro = _context.VaiTro.Where(d => d.IsDeleted == false);

            if (!string.IsNullOrEmpty(searchString))
            {
                dsVaiTro = dsVaiTro.Where(d => d.TenVaiTro.Contains(searchString));
            }

            return View(await dsVaiTro.ToListAsync());
        }

        // 2. CHI TIẾT
        // GET: VaiTro/Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            var vaiTro = await _context.VaiTro
                .FirstOrDefaultAsync(m => m.MaVaiTro == id);
            if (vaiTro == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            return View(vaiTro);
        }

        // 3. TẠO MỚI
        // GET: VaiTro/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: VaiTro/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaVaiTro,TenVaiTro,IsDeleted")] VaiTro vaiTro)
        {
            if (ModelState.IsValid)
            {
                _context.Add(vaiTro);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm mới vai trò thành công!";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "Vui lòng kiểm tra lại thông tin nhập!";
            return View(vaiTro);
        }

        // 4. CHỈNH SỬA
        // GET: VaiTro/Edit
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            var vaiTro = await _context.VaiTro.FindAsync(id);
            if (vaiTro == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }
            return View(vaiTro);
        }

        // POST: VaiTro/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaVaiTro,TenVaiTro,IsDeleted")] VaiTro vaiTro)
        {
            if (id != vaiTro.MaVaiTro)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(vaiTro);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật vai trò thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VaiTroExists(vaiTro.MaVaiTro))
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
            return View(vaiTro);
        }

        // 5. XÓA MỀM (Chuyển vào thùng rác)
        // GET: VaiTro/Delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            var vaiTro = await _context.VaiTro
                .FirstOrDefaultAsync(m => m.MaVaiTro == id);
            if (vaiTro == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            return View(vaiTro);
        }

        // POST: VaiTro/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var vaiTro = await _context.VaiTro.FindAsync(id);
            if (vaiTro != null)
            {
                // Logic xóa mềm
                vaiTro.IsDeleted = true;
                _context.Update(vaiTro);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã chuyển vai trò vào thùng rác.";
            }
            return RedirectToAction(nameof(Index));
        }

        // 6. THÙNG RÁC (Hiện danh sách đã xóa)
        // GET: VaiTro/Trash
        public async Task<IActionResult> Trash(string searchString)
        {
            var dsVaiTro = _context.VaiTro.Where(d => d.IsDeleted == true);

            if (!string.IsNullOrEmpty(searchString))
            {
                dsVaiTro = dsVaiTro.Where(d => d.TenVaiTro.Contains(searchString));
            }
            return View(await dsVaiTro.ToListAsync());
        }

        // 7. KHÔI PHỤC (Hồi sinh từ thùng rác)
        // POST: VaiTro/Restore
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var vaiTro = await _context.VaiTro.FindAsync(id);
            if (vaiTro == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Trash));
            }
            vaiTro.IsDeleted = false;
            _context.Update(vaiTro);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Khôi phục vai trò thành công.";
            return RedirectToAction(nameof(Trash));
        }

        // 8. XÓA VĨNH VIỄN (Chỉ xóa được khi không có ràng buộc)
        // POST: VaiTro/DeleteForce
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteForce(int id)
        {
            var vaiTro = await _context.VaiTro.FindAsync(id);
            if (vaiTro == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Trash));
            }

            try
            {
                _context.VaiTro.Remove(vaiTro);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa vĩnh viễn vai trò.";
                return RedirectToAction(nameof(Trash));
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Không thể xóa vĩnh viễn vai trò này vì đang có nhân viên thuộc vai trò đó!";
                return RedirectToAction(nameof(Trash));
            }
        }

        // 9. DỌN SẠCH THÙNG RÁC (Phiên bản thông minh: Xóa được bao nhiêu thì xóa)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmptyTrash()
        {
            // Lấy tất cả danh sách trong thùng rác
            var racList = await _context.VaiTro.Where(v => v.IsDeleted == true).ToListAsync();

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
                    _context.VaiTro.Remove(item);
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
                TempData["Success"] = $"Đã dọn sạch thùng rác ({daXoa} vai trò).";
            }
            else if (daXoa > 0 && biLoi > 0)
            {
                TempData["Warning"] = $"Đã xóa vĩnh viễn {daXoa} vai trò. Còn lại {biLoi} vai trò không thể xóa do đang được sử dụng.";
            }
            else if (daXoa == 0 && biLoi > 0)
            {
                TempData["Error"] = "Không thể xóa vai trò nào vì tất cả đều đang được sử dụng!";
            }

            return RedirectToAction(nameof(Trash));
        }

        private bool VaiTroExists(int id)
        {
            return _context.VaiTro.Any(e => e.MaVaiTro == id);
        }
    }
}