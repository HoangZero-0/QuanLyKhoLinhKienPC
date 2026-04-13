using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKhoLinhKienPC.Models;
using QuanLyKhoLinhKienPC.ViewModels;
using ClosedXML.Excel;

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

        // XUẤT EXCEL BÁO CÁO NHẬP - XUẤT - TỒN
        // 3. XUẤT EXCEL BÁO CÁO NHẬP - XUẤT - TỒN
        [HttpGet]
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
        public async Task<IActionResult> ExportNhapXuatTon(DateTime? fromDate, DateTime? toDate)
        {
            // Validate ngày hợp lệ cho SQL Server
            var sqlMinDate = new DateTime(1753, 1, 1);
            var sqlMaxDate = new DateTime(9999, 12, 31);

            if (fromDate.HasValue && (fromDate.Value < sqlMinDate || fromDate.Value > sqlMaxDate)) fromDate = null;
            if (toDate.HasValue && (toDate.Value < sqlMinDate || toDate.Value > sqlMaxDate)) toDate = null;

            if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
            {
                TempData["Error"] = "Ngày bắt đầu không thể lớn hơn ngày kết thúc.";
                return RedirectToAction(nameof(Index));
            }

            DateTime kfrom = fromDate.HasValue ? fromDate.Value.Date : sqlMinDate;
            DateTime kto = toDate.HasValue ? toDate.Value.Date.AddDays(1).AddTicks(-1) : sqlMaxDate;

            // Lấy toàn bộ sản phẩm chưa xóa
            var sanPhams = await _context.SanPham
                .Where(sp => !sp.IsDeleted)
                .Include(sp => sp.MaDanhMucNavigation)
                .OrderBy(sp => sp.TenSanPham)
                .ToListAsync();

            // Lấy toàn bộ Seri chưa xóa mềm, kèm navigation
            var allSeri = await _context.SeriSanPham
                .Where(s => !s.IsDeleted)
                .Include(s => s.MaPhieuNhapNavigation)
                .Include(s => s.MaPhieuXuatNavigation)
                .ToListAsync();

            var reportData = new List<(int MaSP, string TenSP, string DanhMuc, int TonDau, int NhapTrongKy, int XuatTrongKy, int TonCuoi)>();

            foreach (var sp in sanPhams)
            {
                var seriOfSP = allSeri.Where(s => s.MaSanPham == sp.MaSanPham).ToList();

                // Tồn đầu kỳ: Seri đã nhập TRƯỚC ngày bắt đầu VÀ (chưa bán HOẶC bán SAU hoặc trong kỳ)
                int tonDau = seriOfSP.Count(s =>
                    s.MaPhieuNhapNavigation != null &&
                    s.MaPhieuNhapNavigation.NgayNhap < kfrom &&
                    (s.MaPhieuXuatNavigation == null || s.MaPhieuXuatNavigation.NgayXuat >= kfrom)
                );

                // Nhập trong kỳ
                int nhapTrongKy = seriOfSP.Count(s =>
                    s.MaPhieuNhapNavigation != null &&
                    s.MaPhieuNhapNavigation.NgayNhap >= kfrom &&
                    s.MaPhieuNhapNavigation.NgayNhap <= kto
                );

                // Xuất trong kỳ
                int xuatTrongKy = seriOfSP.Count(s =>
                    s.MaPhieuXuatNavigation != null &&
                    s.MaPhieuXuatNavigation.NgayXuat >= kfrom &&
                    s.MaPhieuXuatNavigation.NgayXuat <= kto
                );

                int tonCuoi = tonDau + nhapTrongKy - xuatTrongKy;

                // Chỉ hiển thị sản phẩm có số liệu
                if (tonDau > 0 || nhapTrongKy > 0 || xuatTrongKy > 0)
                {
                    reportData.Add((sp.MaSanPham, sp.TenSanPham, sp.MaDanhMucNavigation?.TenDanhMuc ?? "", tonDau, nhapTrongKy, xuatTrongKy, tonCuoi));
                }
            }

            // --- Xuất Excel ---
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("NhapXuatTon");

            ws.Cell(1, 1).Value = "BÁO CÁO NHẬP - XUẤT - TỒN";
            ws.Range("A1:H1").Merge().Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            string kyBaoCao = "Tất cả";
            if (fromDate.HasValue || toDate.HasValue)
                kyBaoCao = $"Từ {fromDate?.ToString("dd/MM/yyyy") ?? "..."} đến {toDate?.ToString("dd/MM/yyyy") ?? "..."}";
            ws.Cell(2, 1).Value = $"Kỳ báo cáo: {kyBaoCao} | Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
            ws.Range("A2:H2").Merge();
            ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(2, 1).Style.Font.Italic = true;

            int headerRow = 4;
            string[] headers = { "STT", "Mã SP", "Tên Sản Phẩm", "Danh Mục", "Tồn đầu kỳ", "Nhập trong kỳ", "Xuất trong kỳ", "Tồn cuối kỳ" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(headerRow, i + 1).Value = headers[i];
            }
            var headerRange = ws.Range(headerRow, 1, headerRow, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            int row = headerRow + 1;
            int stt = 1;
            int sumTonDau = 0, sumNhap = 0, sumXuat = 0, sumTonCuoi = 0;

            foreach (var item in reportData)
            {
                ws.Cell(row, 1).Value = stt;
                ws.Cell(row, 2).Value = item.MaSP;
                ws.Cell(row, 3).Value = item.TenSP;
                ws.Cell(row, 4).Value = item.DanhMuc;
                ws.Cell(row, 5).Value = item.TonDau;
                ws.Cell(row, 6).Value = item.NhapTrongKy;
                ws.Cell(row, 7).Value = item.XuatTrongKy;
                ws.Cell(row, 8).Value = item.TonCuoi;

                // Highlight nếu tồn cuối kỳ = 0
                if (item.TonCuoi <= 0)
                    ws.Range(row, 1, row, headers.Length).Style.Font.FontColor = XLColor.Red;

                sumTonDau += item.TonDau;
                sumNhap += item.NhapTrongKy;
                sumXuat += item.XuatTrongKy;
                sumTonCuoi += item.TonCuoi;
                stt++;
                row++;
            }

            // Dòng TỔNG CỘNG
            ws.Cell(row, 1).Value = "TỔNG CỘNG:";
            ws.Range(row, 1, row, 4).Merge().Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Cell(row, 5).Value = sumTonDau;
            ws.Cell(row, 6).Value = sumNhap;
            ws.Cell(row, 7).Value = sumXuat;
            ws.Cell(row, 8).Value = sumTonCuoi;
            ws.Range(row, 5, row, 8).Style.Font.Bold = true;
            ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#E2EFDA");
            ws.Range(row, 1, row, headers.Length).Style.Border.TopBorder = XLBorderStyleValues.Thin;

            // Viền bảng
            ws.Range(headerRow, 1, row, headers.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range(headerRow, 1, row, headers.Length).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            string fileName = $"Nhap_Xuat_Ton_{DateTime.Now:dd-MM-yyyy_HHmm}.xlsx";
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
