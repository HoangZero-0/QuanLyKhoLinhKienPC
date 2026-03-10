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
    public class NhaCungCapController : Controller
    {
        private readonly QuanLyKhoLinhKienPCContext _context;

        public NhaCungCapController(QuanLyKhoLinhKienPCContext context)
        {
            _context = context;
        }

        // 1. DANH SÁCH (Chỉ hiện cái chưa xóa)
        // GET: NhaCungCap
        public async Task<IActionResult> Index(string searchString)
        {
            var dsNhaCungCap = _context.NhaCungCap.Where(d => d.IsDeleted == false);

            if (!string.IsNullOrEmpty(searchString))
            {
                dsNhaCungCap = dsNhaCungCap.Where(d => d.TenNhaCungCap.Contains(searchString) || d.SoDienThoai.Contains(searchString));
            }

            return View(await dsNhaCungCap.ToListAsync());
        }

        // 2. CHI TIẾT
        // GET: NhaCungCap/Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            var nhaCungCap = await _context.NhaCungCap
                .FirstOrDefaultAsync(m => m.MaNhaCungCap == id);
            if (nhaCungCap == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            return View(nhaCungCap);
        }

        // 3. TẠO MỚI
        // GET: NhaCungCap/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: NhaCungCap/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaNhaCungCap,TenNhaCungCap,SoDienThoai,DiaChi,IsDeleted")] NhaCungCap nhaCungCap)
        {
            if (ModelState.IsValid)
            {
                _context.Add(nhaCungCap);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm mới nhà cung cấp thành công!";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "Vui lòng kiểm tra lại thông tin nhập!";
            return View(nhaCungCap);
        }

        // 4. CHỈNH SỬA
        // GET: NhaCungCap/Edit
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            var nhaCungCap = await _context.NhaCungCap.FindAsync(id);
            if (nhaCungCap == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }
            return View(nhaCungCap);
        }

        // POST: NhaCungCap/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaNhaCungCap,TenNhaCungCap,SoDienThoai,DiaChi,IsDeleted")] NhaCungCap nhaCungCap)
        {
            if (id != nhaCungCap.MaNhaCungCap)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(nhaCungCap);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật nhà cung cấp thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NhaCungCapExists(nhaCungCap.MaNhaCungCap))
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
            return View(nhaCungCap);
        }

        // 5. XÓA MỀM (Chuyển vào thùng rác)
        // GET: NhaCungCap/Delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            var nhaCungCap = await _context.NhaCungCap
                .FirstOrDefaultAsync(m => m.MaNhaCungCap == id);
            if (nhaCungCap == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            return View(nhaCungCap);
        }

        // POST: NhaCungCap/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var nhaCungCap = await _context.NhaCungCap.FindAsync(id);
            if (nhaCungCap != null)
            {
                // Logic xóa mềm
                nhaCungCap.IsDeleted = true;
                _context.Update(nhaCungCap);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã chuyển nhà cung cấp vào thùng rác.";
            }
            return RedirectToAction(nameof(Index));
        }

        // 6. THÙNG RÁC (Hiện danh sách đã xóa)
        // GET: NhaCungCap/Trash
        public async Task<IActionResult> Trash(string searchString)
        {
            var dsNhaCungCap = _context.NhaCungCap.Where(d => d.IsDeleted == true);

            if (!string.IsNullOrEmpty(searchString))
            {
                dsNhaCungCap = dsNhaCungCap.Where(d => d.TenNhaCungCap.Contains(searchString));
            }
            return View(await dsNhaCungCap.ToListAsync());
        }

        // 7. KHÔI PHỤC (Hồi sinh từ thùng rác)
        // POST: NhaCungCap/Restore
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var nhaCungCap = await _context.NhaCungCap.FindAsync(id);
            if (nhaCungCap == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Trash));
            }
            nhaCungCap.IsDeleted = false;
            _context.Update(nhaCungCap);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Khôi phục nhà cung cấp thành công.";
            return RedirectToAction(nameof(Trash));
        }

        // 8. XÓA VĨNH VIỄN (Chỉ xóa được khi không có ràng buộc)
        // POST: NhaCungCap/DeleteForce
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteForce(int id)
        {
            var nhaCungCap = await _context.NhaCungCap.FindAsync(id);
            if (nhaCungCap == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Trash));
            }

            try
            {
                _context.NhaCungCap.Remove(nhaCungCap);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa vĩnh viễn nhà cung cấp.";
                return RedirectToAction(nameof(Trash));
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Không thể xóa vĩnh viễn nhà cung cấp này vì đang có phiếu nhập hàng liên quan!";
                return RedirectToAction(nameof(Trash));
            }
        }

        // 9. DỌN SẠCH THÙNG RÁC (Phiên bản thông minh: Xóa được bao nhiêu thì xóa)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmptyTrash()
        {
            // Lấy tất cả danh sách trong thùng rác
            var racList = await _context.NhaCungCap.Where(d => d.IsDeleted == true).ToListAsync();

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
                    _context.NhaCungCap.Remove(item);
                    await _context.SaveChangesAsync(); // Lưu ngay lập tức
                    daXoa++;
                }
                catch (DbUpdateException)
                {
                    // Nếu lỗi (do ràng buộc khóa ngoại với Phiếu Nhập), bỏ qua và đếm lỗi
                    biLoi++;

                    // QUAN TRỌNG: Phải reset trạng thái của item bị lỗi về "Chưa thay đổi"
                    // Nếu không, EF Core sẽ vẫn nhớ lệnh xóa này và gây lỗi cho item tiếp theo
                    _context.Entry(item).State = EntityState.Unchanged;
                }
            }

            // Thông báo kết quả cho người dùng
            if (daXoa > 0 && biLoi == 0)
            {
                TempData["Success"] = $"Đã dọn sạch thùng rác ({daXoa} nhà cung cấp).";
            }
            else if (daXoa > 0 && biLoi > 0)
            {
                TempData["Warning"] = $"Đã xóa vĩnh viễn {daXoa} nhà cung cấp. Còn lại {biLoi} nhà cung cấp không thể xóa do đang được sử dụng.";
            }
            else if (daXoa == 0 && biLoi > 0)
            {
                TempData["Error"] = "Không thể xóa nhà cung cấp nào vì tất cả đều đang được sử dụng!";
            }

            return RedirectToAction(nameof(Trash));
        }

        private bool NhaCungCapExists(int id)
        {
            return _context.NhaCungCap.Any(e => e.MaNhaCungCap == id);
        }
    }
}