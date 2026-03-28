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
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            var danhMuc = await _context.DanhMuc
                .FirstOrDefaultAsync(m => m.MaDanhMuc == id);
            if (danhMuc == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            return View(danhMuc);
        }

        // 3. TẠO MỚI
        // GET: DanhMuc/Create
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: DanhMuc/Create
        [HttpPost]
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaDanhMuc,TenDanhMuc,IsDeleted")] DanhMuc danhMuc)
        {
            if (ModelState.IsValid)
            {
                _context.Add(danhMuc);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm mới danh mục thành công!";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "Vui lòng kiểm tra lại thông tin nhập!";
            return View(danhMuc);
        }

        // 4. CHỈNH SỬA
        // GET: DanhMuc/Edit
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            var danhMuc = await _context.DanhMuc.FindAsync(id);
            if (danhMuc == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }
            return View(danhMuc);
        }

        // POST: DanhMuc/Edit
        [HttpPost]
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaDanhMuc,TenDanhMuc,IsDeleted")] DanhMuc danhMuc)
        {
            if (id != danhMuc.MaDanhMuc)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(danhMuc);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật danh mục thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DanhMucExists(danhMuc.MaDanhMuc))
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
            return View(danhMuc);
        }

        // 5. XÓA MỀM (Chuyển vào thùng rác)
        // GET: DanhMuc/Delete
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            var danhMuc = await _context.DanhMuc
                .FirstOrDefaultAsync(m => m.MaDanhMuc == id);
            if (danhMuc == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            return View(danhMuc);
        }

        // POST: DanhMuc/Delete
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
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
                TempData["Success"] = "Đã chuyển danh mục vào thùng rác.";
            }
            return RedirectToAction(nameof(Index));
        }

        // 6. THÙNG RÁC (Hiện danh sách đã xóa)
        // GET: DanhMuc/Trash
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
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
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var danhMuc = await _context.DanhMuc.FindAsync(id);
            if (danhMuc == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Trash));
            }
            danhMuc.IsDeleted = false;
            _context.Update(danhMuc);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Khôi phục danh mục thành công.";
            return RedirectToAction(nameof(Trash));
        }

        private bool DanhMucExists(int id)
        {
            return _context.DanhMuc.Any(e => e.MaDanhMuc == id);
        }
    }
}