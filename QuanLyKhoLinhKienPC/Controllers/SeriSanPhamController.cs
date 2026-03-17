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
    public class SeriSanPhamController : Controller
    {
        private readonly QuanLyKhoLinhKienPCContext _context;

        public SeriSanPhamController(QuanLyKhoLinhKienPCContext context)
        {
            _context = context;
        }

        // 1. DANH SÁCH (Sắp xếp theo phiếu nhập mới nhất)
        // GET: SeriSanPham
        public async Task<IActionResult> Index(int? trangThaiFilter, string searchString)
        {
            var seriList = _context.SeriSanPham
                .Include(s => s.MaSanPhamNavigation)
                .Include(s => s.MaPhieuNhapNavigation)
                .Where(s => !s.IsDeleted);

            // Tìm kiếm theo chuỗi Seri
            if (!string.IsNullOrEmpty(searchString))
            {
                seriList = seriList.Where(s => s.SoSeri.Contains(searchString) ||
                                               s.MaSanPhamNavigation.TenSanPham.Contains(searchString) ||
                                               s.MaSanPhamNavigation.HangSanXuat.Contains(searchString));
            }

            // Lọc theo trạng thái Tồn Kho / Đã Bán
            if (trangThaiFilter.HasValue)
            {
                seriList = seriList.Where(s => s.TrangThai == trangThaiFilter.Value);
            }

            // Mặc định xem Seri mới nhất sinh ra
            seriList = seriList.OrderByDescending(s => s.MaPhieuNhap).ThenBy(s => s.SoSeri);

            ViewData["CurrentFilter"] = searchString;
            ViewData["TrangThaiFilter"] = trangThaiFilter;

            return View(await seriList.ToListAsync());
        }

        // 2. CHI TIẾT (Truy vết chi tiết giao dịch của 1 Seri)
        // GET: SeriSanPham/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            var seri = await _context.SeriSanPham
                .Include(s => s.MaSanPhamNavigation)
                .Include(s => s.MaPhieuNhapNavigation)
                    .ThenInclude(pn => pn.MaNguoiDungNavigation)
                .Include(s => s.MaPhieuNhapNavigation)
                    .ThenInclude(pn => pn.MaNhaCungCapNavigation)
                .Include(s => s.MaPhieuXuatNavigation)
                    .ThenInclude(px => px.MaNguoiDungNavigation)
                .FirstOrDefaultAsync(m => m.MaSeri == id);

            if (seri == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            return View(seri);
        }
    }
}
