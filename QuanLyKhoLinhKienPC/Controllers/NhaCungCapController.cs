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
        // GET: NhaCungCap/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var nhaCungCap = await _context.NhaCungCap
                .FirstOrDefaultAsync(m => m.MaNhaCungCap == id);
            if (nhaCungCap == null) return NotFound();

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
            // Kiểm tra trùng tên
            bool isDuplicate = _context.NhaCungCap.Any(d => d.TenNhaCungCap.Trim().ToLower() == nhaCungCap.TenNhaCungCap.Trim().ToLower() && d.IsDeleted == false);
            if (isDuplicate)
            {
                ModelState.AddModelError("TenNhaCungCap", "Nhà cung cấp này đã tồn tại!");
            }

            if (ModelState.IsValid)
            {
                _context.Add(nhaCungCap);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(nhaCungCap);
        }

        // 4. CHỈNH SỬA
        // GET: NhaCungCap/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var nhaCungCap = await _context.NhaCungCap.FindAsync(id);
            if (nhaCungCap == null) return NotFound();
            return View(nhaCungCap);
        }

        // POST: NhaCungCap/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaNhaCungCap,TenNhaCungCap,SoDienThoai,DiaChi,IsDeleted")] NhaCungCap nhaCungCap)
        {
            if (id != nhaCungCap.MaNhaCungCap) return NotFound();

            // Kiểm tra trùng tên (trừ chính nó)
            bool isDuplicate = _context.NhaCungCap.Any(d =>
                d.TenNhaCungCap.Trim().ToLower() == nhaCungCap.TenNhaCungCap.Trim().ToLower()
                && d.MaNhaCungCap != nhaCungCap.MaNhaCungCap
                && d.IsDeleted == false);

            if (isDuplicate)
            {
                ModelState.AddModelError("TenNhaCungCap", "Tên nhà cung cấp này đã bị trùng!");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(nhaCungCap);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NhaCungCapExists(nhaCungCap.MaNhaCungCap)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(nhaCungCap);
        }

        // 5. XÓA MỀM
        // GET: NhaCungCap/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var nhaCungCap = await _context.NhaCungCap
                .FirstOrDefaultAsync(m => m.MaNhaCungCap == id);
            if (nhaCungCap == null) return NotFound();

            return View(nhaCungCap);
        }

        // POST: NhaCungCap/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var nhaCungCap = await _context.NhaCungCap.FindAsync(id);
            if (nhaCungCap != null)
            {
                nhaCungCap.IsDeleted = true; // Xóa mềm
                _context.Update(nhaCungCap);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // 6. THÙNG RÁC
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

        // 7. KHÔI PHỤC
        // POST: NhaCungCap/Restore/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var nhaCungCap = await _context.NhaCungCap.FindAsync(id);
            if (nhaCungCap == null) return NotFound();

            nhaCungCap.IsDeleted = false;
            _context.Update(nhaCungCap);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Trash));
        }

        // 8. XÓA VĨNH VIỄN
        // POST: NhaCungCap/DeleteForce/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteForce(int id)
        {
            var nhaCungCap = await _context.NhaCungCap.FindAsync(id);
            if (nhaCungCap == null) return NotFound();

            try
            {
                _context.NhaCungCap.Remove(nhaCungCap);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Trash));
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Không thể xóa nhà cung cấp này vì đã có dữ liệu nhập hàng liên quan!";
                return RedirectToAction(nameof(Trash));
            }
        }

        private bool NhaCungCapExists(int id)
        {
            return _context.NhaCungCap.Any(e => e.MaNhaCungCap == id);
        }
    }
}