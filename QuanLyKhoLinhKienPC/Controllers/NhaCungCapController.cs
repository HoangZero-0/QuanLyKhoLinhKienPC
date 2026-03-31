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
    [Authorize]
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
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: NhaCungCap/Create
        [HttpPost]
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaNhaCungCap,TenNhaCungCap,SoDienThoai,DiaChi,IsDeleted")] NhaCungCap nhaCungCap)
        {
            if (ModelState.IsValid)
            {
                _context.Add(nhaCungCap);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm mới Nhà Cung Cấp thành công!";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "Vui lòng kiểm tra lại thông tin nhập!";
            return View(nhaCungCap);
        }

        // 4. CHỈNH SỬA
        // GET: NhaCungCap/Edit
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
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
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
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
                    TempData["Success"] = "Cập nhật Nhà Cung Cấp thành công!";
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
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
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
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
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
                TempData["Success"] = "Đã chuyển Nhà Cung Cấp vào thùng rác.";
            }
            return RedirectToAction(nameof(Index));
        }

        // 6. THÙNG RÁC (Hiện danh sách đã xóa)
        // GET: NhaCungCap/Trash
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
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
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
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
            TempData["Success"] = "Khôi phục Nhà Cung Cấp thành công.";
            return RedirectToAction(nameof(Trash));
        }

        private bool NhaCungCapExists(int id)
        {
            return _context.NhaCungCap.Any(e => e.MaNhaCungCap == id);
        }
    }
}