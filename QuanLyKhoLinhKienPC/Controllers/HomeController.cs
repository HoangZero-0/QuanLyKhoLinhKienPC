using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhoLinhKienPC.Models;
using QuanLyKhoLinhKienPC.ViewModels;

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

            ViewData["TongDoanhThu"] = tongDoanhThu;
            ViewData["SoDonHang"] = soDonHang;
            ViewData["TongSoSanPham"] = tongSoSanPham;
            ViewData["TongTonKho"] = tongTonKho;

            // 1. Cảnh báo hết hàng (Tồn kho < 3)
            var spHetHang = await _context.SanPham
                .Where(s => !s.IsDeleted)
                .Select(s => new
                {
                    SanPham = s,
                    TonKho = s.SeriSanPham.Count(sr => sr.TrangThai == 1 && !sr.IsDeleted)
                })
                .Where(x => x.TonKho < 3)
                .OrderBy(x => x.TonKho)
                .ToListAsync();

            ViewData["CanhBaoHetHang"] = spHetHang.Select(x => new
            {
                MaSanPham = x.SanPham.MaSanPham,
                TenSanPham = x.SanPham.TenSanPham,
                TonKho = x.TonKho
            }).ToList();

            // 2. Hoạt động gần đây (Tất cả hoạt động hệ thống từ NhatKyHoatDong)
            var lastLogs = await _context.NhatKyHoatDong
                .Include(nk => nk.MaNguoiDungNavigation)
                .OrderByDescending(nk => nk.ThoiGian)
                .Take(30)
                .ToListAsync();

            var listHoatDong = lastLogs.Select(nk => new HoatDongVM
            {
                LoaiHoatDong = nk.LoaiHanhDong,
                Icon = nk.LoaiHanhDong == "Thêm mới" ? "fa-plus-circle" :
                       nk.LoaiHanhDong == "Cập nhật" ? "fa-pen-to-square" :
                       nk.LoaiHanhDong == "Khôi phục" ? "fa-trash-can-arrow-up" : "fa-trash",
                ColorClass = nk.LoaiHanhDong == "Thêm mới" ? "text-success bg-success" :
                             nk.LoaiHanhDong == "Cập nhật" ? "text-primary bg-primary" :
                             nk.LoaiHanhDong == "Khôi phục" ? "text-info bg-info" : "text-danger bg-danger",
                ThoiGian = nk.ThoiGian ?? DateTime.Now,
                NguoiThucHien = nk.MaNguoiDungNavigation?.HoTen ?? "Hệ thống",
                MoTa = nk.MoTaChiTiet
            }).ToList();

            ViewData["HoatDongGanDay"] = listHoatDong;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? id)
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

            if (id == 404)
            {
                ViewData["ErrorMessage"] = "Rất tiếc, trang bạn đang tìm kiếm không tồn tại hoặc đã bị di dời.";
                ViewData["ErrorTitle"] = "Không Tìm Thấy Trang";
            }
            else if (id == 403)
            {
                ViewData["ErrorMessage"] = "Bạn không có quyền truy cập vào khu vực này.";
                ViewData["ErrorTitle"] = "Truy Cập Bị Từ Chối";
            }
            else
            {
                ViewData["ErrorMessage"] = "Đã có lỗi xảy ra trong quá trình xử lý yêu cầu của bạn.";
                ViewData["ErrorTitle"] = "Lỗi Hệ Thống";
            }

            return View(new ErrorViewModel { RequestId = requestId });
        }
    }
}
