using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyKhoLinhKienPC.Models;

namespace QuanLyKhoLinhKienPC.Controllers
{
    public class DanhMucController : Controller
    {
        private readonly QuanLyKhoLinhKienPCContext _context;

        public DanhMucController(QuanLyKhoLinhKienPCContext context)
        {
            _context = context;
        }

        // 1. DANH SÁCH (Chỉ hiện cái chưa xóa)
        // GET: DanhMuc
        public async Task<IActionResult> Index(string searchString)
        {
            var dsDanhMuc = _context.DanhMuc.Where(d => d.IsDeleted == false);

            if (!string.IsNullOrEmpty(searchString))
            {
                dsDanhMuc = dsDanhMuc.Where(d => d.TenDanhMuc.Contains(searchString));
            }

            return View(await dsDanhMuc.ToListAsync());
        }

        // 2. CHI TIẾT
        // GET: DanhMuc/Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var danhMuc = await _context.DanhMuc
                .FirstOrDefaultAsync(m => m.MaDanhMuc == id);
            if (danhMuc == null)
            {
                return NotFound();
            }

            return View(danhMuc);
        }

        // 3. TẠO MỚI
        // GET: DanhMuc/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: DanhMuc/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaDanhMuc,TenDanhMuc,IsDeleted")] DanhMuc danhMuc)
        {
            // Kiểm tra trùng tên
            bool isDuplicate = _context.DanhMuc.Any(d => d.TenDanhMuc.Trim().ToLower() == danhMuc.TenDanhMuc.Trim().ToLower() && d.IsDeleted == false);
            if (isDuplicate)
            {
                ModelState.AddModelError("TenDanhMuc", "Tên danh mục này đã tồn tại trong hệ thống!");
            }

            if (ModelState.IsValid)
            {
                _context.Add(danhMuc);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(danhMuc);
        }

        // 4. CHỈNH SỬA
        // GET: DanhMuc/Edit
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var danhMuc = await _context.DanhMuc.FindAsync(id);
            if (danhMuc == null)
            {
                return NotFound();
            }
            return View(danhMuc);
        }

        // POST: DanhMuc/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaDanhMuc,TenDanhMuc,IsDeleted")] DanhMuc danhMuc)
        {
            if (id != danhMuc.MaDanhMuc)
            {
                return NotFound();
            }

            // Kiểm tra trùng tên (trừ chính nó ra)
            bool isDuplicate = _context.DanhMuc.Any(d =>
                d.TenDanhMuc.Trim().ToLower() == danhMuc.TenDanhMuc.Trim().ToLower()
                && d.MaDanhMuc != danhMuc.MaDanhMuc
                && d.IsDeleted == false);

            if (isDuplicate)
            {
                ModelState.AddModelError("TenDanhMuc", "Tên danh mục này đã bị trùng với một danh mục khác!");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(danhMuc);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DanhMucExists(danhMuc.MaDanhMuc))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(danhMuc);
        }

        // 5. XÓA MỀM (Chuyển vào thùng rác)
        // GET: DanhMuc/Delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var danhMuc = await _context.DanhMuc
                .FirstOrDefaultAsync(m => m.MaDanhMuc == id);
            if (danhMuc == null)
            {
                return NotFound();
            }

            return View(danhMuc);
        }

        // POST: DanhMuc/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var danhMuc = await _context.DanhMuc.FindAsync(id);
            if (danhMuc != null)
            {
                // Logic xóa mềm
                danhMuc.IsDeleted = true;
                _context.Update(danhMuc);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // 6. THÙNG RÁC (Hiện danh sách đã xóa)
        // GET: DanhMuc/Trash
        public async Task<IActionResult> Trash(string searchString)
        {
            var dsDanhMuc = _context.DanhMuc.Where(d => d.IsDeleted == true);

            if (!string.IsNullOrEmpty(searchString))
            {
                dsDanhMuc = dsDanhMuc.Where(d => d.TenDanhMuc.Contains(searchString));
            }
            return View(await dsDanhMuc.ToListAsync());
        }

        // 7. KHÔI PHỤC (Hồi sinh từ thùng rác)
        // POST: DanhMuc/Restore
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var danhMuc = await _context.DanhMuc.FindAsync(id);
            if (danhMuc == null)
            {
                return NotFound();
            }
            danhMuc.IsDeleted = false;
            _context.Update(danhMuc);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Trash));
        }

        // 8. XÓA VĨNH VIỄN (Chỉ xóa được khi không có ràng buộc)
        // POST: DanhMuc/DeleteForce
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteForce(int id)
        {
            var danhMuc = await _context.DanhMuc.FindAsync(id);
            if (danhMuc == null) return NotFound();

            try
            {
                _context.DanhMuc.Remove(danhMuc);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Trash));
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Không thể xóa vĩnh viễn danh mục này vì đang có sản phẩm thuộc danh mục đó!";
                return RedirectToAction(nameof(Trash));
            }
        }

        // 9. DỌN SẠCH THÙNG RÁC (Phiên bản thông minh: Xóa được bao nhiêu thì xóa)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmptyTrash()
        {
            // Lấy tất cả danh sách trong thùng rác
            var racList = await _context.DanhMuc.Where(d => d.IsDeleted == true).ToListAsync();

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
                    _context.DanhMuc.Remove(item);
                    await _context.SaveChangesAsync(); // Lưu ngay lập tức
                    daXoa++;
                }
                catch (DbUpdateException)
                {
                    // Nếu lỗi (do ràng buộc khóa ngoại với Sản phẩm), bỏ qua và đếm lỗi
                    biLoi++;

                    /// QUAN TRỌNG: Phải reset trạng thái của item bị lỗi về "Chưa thay đổi"
                    // Nếu không, EF Core sẽ vẫn nhớ lệnh xóa này và gây lỗi cho item tiếp theo
                    _context.Entry(item).State = EntityState.Unchanged;
                }
            }

            // Thông báo kết quả cho người dùng
            if (daXoa > 0 && biLoi == 0)
            {
                TempData["Success"] = $"Đã dọn sạch thùng rác ({daXoa} danh mục).";
            }
            else if (daXoa > 0 && biLoi > 0)
            {
                TempData["Warning"] = $"Đã xóa vĩnh viễn {daXoa} danh mục. Còn lại {biLoi} danh mục không thể xóa do đang được sử dụng.";
            }
            else if (daXoa == 0 && biLoi > 0)
            {
                TempData["Error"] = "Không thể xóa danh mục nào vì tất cả đều đang được sử dụng!";
            }

            return RedirectToAction(nameof(Trash));
        }

        private bool DanhMucExists(int id)
        {
            return _context.DanhMuc.Any(e => e.MaDanhMuc == id);
        }        
    }
}