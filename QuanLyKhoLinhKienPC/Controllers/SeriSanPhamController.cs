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
    public class SeriSanPhamController : Controller
    {
        private readonly QuanLyKhoLinhKienPCContext _context;

        public SeriSanPhamController(QuanLyKhoLinhKienPCContext context)
        {
            _context = context;
        }

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

        // Action Tạm thời chưa viết các chức năng Edit thủ công vì Seri là bất biến (hệ thống sinh ra)

        // GET: SeriSanPham/Details/5 (TRUY VẾT)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
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
                return NotFound();
            }

            return View(seri);
        }
    }
}
