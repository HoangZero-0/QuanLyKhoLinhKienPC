using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhoLinhKienPC.Models;

namespace QuanLyKhoLinhKienPC.Controllers
{
    [Authorize] // Bảo mật trang Dashboard
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly QuanLyKhoLinhKienPCContext _context;

        public HomeController(ILogger<HomeController> logger, QuanLyKhoLinhKienPCContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Thống kê Doanh Thu (từ Phiếu Xuất không bị xóa)
            var tongDoanhThu = await _context.PhieuXuat
                .Where(p => !p.IsDeleted)
                .SumAsync(p => p.TongTien);

            // Thống kê Số Hóa Đơn (từ Phiếu Xuất không bị xóa)
            var soDonHang = await _context.PhieuXuat
                .Where(p => !p.IsDeleted)
                .CountAsync();

            // Tổng số Model Sản phẩm
            var tongSoSanPham = await _context.SanPham
                .Where(s => !s.IsDeleted)
                .CountAsync();
                
            // Số linh kiện (Seri) đang có sẵn trong kho (TrangThai = 1 và không bị xóa)
            var tongTonKho = await _context.SeriSanPham
                .Where(s => s.TrangThai == 1 && !s.IsDeleted)
                .CountAsync();

            ViewBag.TongDoanhThu = tongDoanhThu;
            ViewBag.SoDonHang = soDonHang;
            ViewBag.TongSoSanPham = tongSoSanPham;
            ViewBag.TongTonKho = tongTonKho;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
