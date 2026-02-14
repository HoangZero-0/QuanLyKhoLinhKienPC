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
    public class PhieuXuatController : Controller
    {
        private readonly QuanLyKhoLinhKienPCContext _context;

        public PhieuXuatController(QuanLyKhoLinhKienPCContext context)
        {
            _context = context;
        }

        // GET: PhieuXuat
        public async Task<IActionResult> Index()
        {
            var quanLyKhoLinhKienPCContext = _context.PhieuXuat.Include(p => p.MaNguoiDungNavigation);
            return View(await quanLyKhoLinhKienPCContext.ToListAsync());
        }

        // GET: PhieuXuat/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phieuXuat = await _context.PhieuXuat
                .Include(p => p.MaNguoiDungNavigation)
                .FirstOrDefaultAsync(m => m.MaPhieuXuat == id);
            if (phieuXuat == null)
            {
                return NotFound();
            }

            return View(phieuXuat);
        }

        // GET: PhieuXuat/Create
        public IActionResult Create()
        {
            ViewData["MaNguoiDung"] = new SelectList(_context.NguoiDung, "MaNguoiDung", "MatKhau");
            return View();
        }

        // POST: PhieuXuat/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaPhieuXuat,NgayXuat,TenKhachHang,SoDienThoaiKhach,TongTien,MaNguoiDung,IsDeleted")] PhieuXuat phieuXuat)
        {
            if (ModelState.IsValid)
            {
                _context.Add(phieuXuat);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaNguoiDung"] = new SelectList(_context.NguoiDung, "MaNguoiDung", "MatKhau", phieuXuat.MaNguoiDung);
            return View(phieuXuat);
        }

        // GET: PhieuXuat/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phieuXuat = await _context.PhieuXuat.FindAsync(id);
            if (phieuXuat == null)
            {
                return NotFound();
            }
            ViewData["MaNguoiDung"] = new SelectList(_context.NguoiDung, "MaNguoiDung", "MatKhau", phieuXuat.MaNguoiDung);
            return View(phieuXuat);
        }

        // POST: PhieuXuat/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaPhieuXuat,NgayXuat,TenKhachHang,SoDienThoaiKhach,TongTien,MaNguoiDung,IsDeleted")] PhieuXuat phieuXuat)
        {
            if (id != phieuXuat.MaPhieuXuat)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(phieuXuat);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PhieuXuatExists(phieuXuat.MaPhieuXuat))
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
            ViewData["MaNguoiDung"] = new SelectList(_context.NguoiDung, "MaNguoiDung", "MatKhau", phieuXuat.MaNguoiDung);
            return View(phieuXuat);
        }

        // GET: PhieuXuat/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var phieuXuat = await _context.PhieuXuat
                .Include(p => p.MaNguoiDungNavigation)
                .FirstOrDefaultAsync(m => m.MaPhieuXuat == id);
            if (phieuXuat == null)
            {
                return NotFound();
            }

            return View(phieuXuat);
        }

        // POST: PhieuXuat/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var phieuXuat = await _context.PhieuXuat.FindAsync(id);
            if (phieuXuat != null)
            {
                _context.PhieuXuat.Remove(phieuXuat);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PhieuXuatExists(int id)
        {
            return _context.PhieuXuat.Any(e => e.MaPhieuXuat == id);
        }
    }
}
