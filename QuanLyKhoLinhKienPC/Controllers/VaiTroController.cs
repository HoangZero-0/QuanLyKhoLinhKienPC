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
        // GET: VaiTro/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vaiTro = await _context.VaiTro
                .FirstOrDefaultAsync(m => m.MaVaiTro == id);
            if (vaiTro == null)
            {
                return NotFound();
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
            // Kiểm tra trùng tên
            bool isDuplicate = _context.VaiTro.Any(d => d.TenVaiTro.Trim().ToLower() == vaiTro.TenVaiTro.Trim().ToLower() && d.IsDeleted == false);
            if (isDuplicate)
            {
                ModelState.AddModelError("TenVaiTro", "Tên vai trò này đã tồn tại trong hệ thống!");
            }

            if (ModelState.IsValid)
            {
                _context.Add(vaiTro);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(vaiTro);
        }

        // 4. CHỈNH SỬA
        // GET: VaiTro/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vaiTro = await _context.VaiTro.FindAsync(id);
            if (vaiTro == null)
            {
                return NotFound();
            }
            return View(vaiTro);
        }

        // POST: VaiTro/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaVaiTro,TenVaiTro,IsDeleted")] VaiTro vaiTro)
        {
            if (id != vaiTro.MaVaiTro)
            {
                return NotFound();
            }

            // Kiểm tra trùng tên (trừ chính nó ra)
            bool isDuplicate = _context.VaiTro.Any(d =>
                d.TenVaiTro.Trim().ToLower() == vaiTro.TenVaiTro.Trim().ToLower()
                && d.MaVaiTro != vaiTro.MaVaiTro
                && d.IsDeleted == false);

            if (isDuplicate)
            {
                ModelState.AddModelError("TenVaiTro", "Tên vai trò này đã bị trùng với một vai trò khác!");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(vaiTro);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VaiTroExists(vaiTro.MaVaiTro))
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
            return View(vaiTro);
        }

        // 5. XÓA MỀM (Chuyển vào thùng rác)
        // GET: VaiTro/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vaiTro = await _context.VaiTro
                .FirstOrDefaultAsync(m => m.MaVaiTro == id);
            if (vaiTro == null)
            {
                return NotFound();
            }

            return View(vaiTro);
        }

        // POST: VaiTro/Delete/5
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
        // POST: VaiTro/Restore/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var vaiTro = await _context.VaiTro.FindAsync(id);
            if (vaiTro == null)
            {
                return NotFound();
            }
            vaiTro.IsDeleted = false;
            _context.Update(vaiTro);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Trash));
        }

        // 8. XÓA VĨNH VIỄN (Chỉ xóa được khi không có ràng buộc)
        // POST: VaiTro/DeleteForce/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteForce(int id)
        {
            var vaiTro = await _context.VaiTro.FindAsync(id);
            if (vaiTro == null) return NotFound();

            try
            {
                _context.VaiTro.Remove(vaiTro);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Trash));
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Không thể xóa vĩnh viễn vai trò này vì đang có nhân viên thuộc vai trò đó!";
                return RedirectToAction(nameof(Trash));
            }
        }

        private bool VaiTroExists(int id)
        {
            return _context.VaiTro.Any(e => e.MaVaiTro == id);
        }
    }
}