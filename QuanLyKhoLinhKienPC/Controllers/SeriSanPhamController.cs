using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyKhoLinhKienPC.Models;
using Microsoft.AspNetCore.Authorization;
using QuanLyKhoLinhKienPC.Helpers;
using System.Security.Claims;
using ClosedXML.Excel;

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
        public async Task<IActionResult> Index(int? trangThaiFilter, string searchString, DateTime? fromDate, DateTime? toDate)
        {
            var seriList = _context.SeriSanPham
                .Include(s => s.MaSanPhamNavigation)
                    .ThenInclude(sp => sp.MaDanhMucNavigation)
                .Include(s => s.MaPhieuNhapNavigation)
                    .ThenInclude(pn => pn.ChiTietPhieuNhap)
                .Include(s => s.MaPhieuXuatNavigation)
                    .ThenInclude(px => px.ChiTietPhieuXuat)
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

            // Validate ngày hợp lệ cho SQL Server (phạm vi: 1753 -> 9999)
            var sqlMinDate = new DateTime(1753, 1, 1);
            var sqlMaxDate = new DateTime(9999, 12, 31);
            if (fromDate.HasValue && (fromDate.Value < sqlMinDate || fromDate.Value > sqlMaxDate))
            {
                TempData["Error"] = "Thời gian bắt đầu không hợp lệ!";
                fromDate = null;
            }
            if (toDate.HasValue && (toDate.Value < sqlMinDate || toDate.Value > sqlMaxDate))
            {
                TempData["Error"] = "Thời gian kết thúc không hợp lệ!";
                toDate = null;
            }

            // Lọc theo khoảng ngày
            if (fromDate.HasValue || toDate.HasValue)
            {
                // Kiểm tra logic thời gian cơ bản
                if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
                {
                    TempData["Error"] = "Khoảng thời gian không hợp lệ (Từ Ngày lớn hơn Đến Ngày). Đã hủy lọc!";
                    fromDate = null;
                    toDate = null;
                }
                else
                {
                    DateTime? fDate = fromDate.HasValue ? fromDate.Value.Date : null;
                    DateTime? tDate = toDate.HasValue ? toDate.Value.Date.AddDays(1).AddTicks(-1) : null;

                    if (trangThaiFilter == 1)
                    {
                        if (fDate.HasValue) seriList = seriList.Where(s => s.MaPhieuNhapNavigation != null && s.MaPhieuNhapNavigation.NgayNhap >= fDate.Value);
                        if (tDate.HasValue) seriList = seriList.Where(s => s.MaPhieuNhapNavigation != null && s.MaPhieuNhapNavigation.NgayNhap <= tDate.Value);
                    }
                    else if (trangThaiFilter == 2)
                    {
                        if (fDate.HasValue) seriList = seriList.Where(s => s.MaPhieuXuatNavigation != null && s.MaPhieuXuatNavigation.NgayXuat >= fDate.Value);
                        if (tDate.HasValue) seriList = seriList.Where(s => s.MaPhieuXuatNavigation != null && s.MaPhieuXuatNavigation.NgayXuat <= tDate.Value);
                    }
                    else if (trangThaiFilter == 3)
                    {
                        if (fDate.HasValue) seriList = seriList.Where(s =>
                            (s.MaPhieuXuat == null && s.MaPhieuNhapNavigation != null && s.MaPhieuNhapNavigation.NgayNhap >= fDate.Value) ||
                            (s.MaPhieuXuat != null && s.MaPhieuXuatNavigation != null && s.MaPhieuXuatNavigation.NgayXuat >= fDate.Value)
                        );
                        if (tDate.HasValue) seriList = seriList.Where(s =>
                            (s.MaPhieuXuat == null && s.MaPhieuNhapNavigation != null && s.MaPhieuNhapNavigation.NgayNhap <= tDate.Value) ||
                            (s.MaPhieuXuat != null && s.MaPhieuXuatNavigation != null && s.MaPhieuXuatNavigation.NgayXuat <= tDate.Value)
                        );
                    }
                    else
                    {
                        // Lọc mặc định theo NgayNhap nếu không chọn trạng thái
                        if (fDate.HasValue) seriList = seriList.Where(s => s.MaPhieuNhapNavigation != null && s.MaPhieuNhapNavigation.NgayNhap >= fDate.Value);
                        if (tDate.HasValue) seriList = seriList.Where(s => s.MaPhieuNhapNavigation != null && s.MaPhieuNhapNavigation.NgayNhap <= tDate.Value);
                    }
                }
            }

            // Mặc định xem Seri mới nhất sinh ra
            seriList = seriList.OrderByDescending(s => s.MaPhieuNhap).ThenBy(s => s.SoSeri);

            ViewData["CurrentFilter"] = searchString;
            ViewData["TrangThaiFilter"] = trangThaiFilter;
            ViewData["FromDate"] = fromDate?.ToString("yyyy-MM-dd");
            ViewData["ToDate"] = toDate?.ToString("yyyy-MM-dd");

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

            decimal donGiaNhap = 0;
            if (seri.MaPhieuNhap != null)
            {
                var chiTiet = await _context.ChiTietPhieuNhap
                    .FirstOrDefaultAsync(ct => ct.MaPhieuNhap == seri.MaPhieuNhap && ct.MaSanPham == seri.MaSanPham);
                if (chiTiet != null)
                {
                    donGiaNhap = chiTiet.DonGiaNhap;
                }
            }
            ViewData["DonGiaNhap"] = donGiaNhap;

            // Lấy đơn giá xuất (giá bán) từ ChiTietPhieuXuat
            decimal donGiaXuat = 0;
            if (seri.MaPhieuXuat != null)
            {
                var chiTietXuat = await _context.ChiTietPhieuXuat
                    .FirstOrDefaultAsync(ctx => ctx.MaPhieuXuat == seri.MaPhieuXuat && ctx.MaSeri == seri.MaSeri);
                if (chiTietXuat != null)
                {
                    donGiaXuat = chiTietXuat.GiaTien;
                }
            }
            ViewData["DonGiaXuat"] = donGiaXuat;

            return View(seri);
        }

        // 3. THAY ĐỔI TRẠNG THÁI LỖI / BẢO HÀNH
        // POST: SeriSanPham/ToggleDefect/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleDefect(int id)
        {
            var seri = await _context.SeriSanPham
                .Include(s => s.MaSanPhamNavigation)
                .Include(s => s.MaPhieuXuatNavigation)
                .FirstOrDefaultAsync(s => s.MaSeri == id);

            if (seri == null)
            {
                TempData["Error"] = "Không tìm thấy lỗi Seri!";
                return RedirectToAction(nameof(Index));
            }

            if (seri.TrangThai == 1) // Đang tồn kho -> Báo lỗi
            {
                seri.TrangThai = 3;
                TempData["Success"] = $"Đã báo lỗi cho Seri: {seri.SoSeri}.";
            }
            else if (seri.TrangThai == 3) // Đang lỗi -> Đã sửa xong -> Về tồn kho HOẶC Đã Bán
            {
                if (seri.MaPhieuXuat == null)
                {
                    seri.TrangThai = 1; // Chưa từng bán -> Về tồn kho
                    TempData["Success"] = $"Seri: {seri.SoSeri} đã sửa/bảo hành thành công.";
                }
                else
                {
                    seri.TrangThai = 2; // Đã từng bán -> Trả lại khách -> Về Đã Bán
                    TempData["Success"] = $"Seri: {seri.SoSeri} đã hoàn tất bảo hành và bàn giao lại cho khách hàng.";
                }
            }
            else if (seri.TrangThai == 2) // Đã bán -> Khách mang tới bảo hành
            {
                if (seri.MaPhieuXuatNavigation != null)
                {
                    int thoiGianBaoHanh = seri.MaSanPhamNavigation?.ThoiGianBaoHanh ?? 0;
                    DateTime ngayHetHan = seri.MaPhieuXuatNavigation.NgayXuat.AddMonths(thoiGianBaoHanh);

                    if (DateTime.Now <= ngayHetHan)
                    {
                        seri.TrangThai = 3;
                        TempData["Success"] = $"Đã tiếp nhận bảo hành Seri: {seri.SoSeri} (còn hạn bảo hành đến {ngayHetHan:dd/MM/yyyy}).";
                    }
                    else
                    {
                        seri.TrangThai = 3;
                        TempData["Warning"] = $"Seri: {seri.SoSeri} đã hết hạn bảo hành từ {ngayHetHan:dd/MM/yyyy}. Đã nhận phiếu sửa chữa dịch vụ tính phí.";
                    }
                }
            }

            _context.Update(seri);
            await _context.SaveChangesAsync();
            await ActivityLogger.LogAsync(_context, int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1"), "Cập nhật", "Seri Sản Phẩm", $"Sửa trạng thái Seri: {seri.SoSeri}");

            // Trích xuất filter hiện tại để quay lại đúng trạng thái đang ở
            string referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer))
            {
                return Redirect(referer);
            }
            return RedirectToAction(nameof(Index));
        }

        // 4. XÓA MỀM (Chuyển vào thùng rác)
        // GET: SeriSanPham/Delete/5
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            var seri = await _context.SeriSanPham
                .Include(s => s.MaSanPhamNavigation)
                .Include(s => s.MaPhieuNhapNavigation)
                .FirstOrDefaultAsync(m => m.MaSeri == id);

            if (seri == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            decimal donGiaNhap = 0;
            if (seri.MaPhieuNhap != null)
            {
                var chiTiet = await _context.ChiTietPhieuNhap
                    .FirstOrDefaultAsync(ct => ct.MaPhieuNhap == seri.MaPhieuNhap && ct.MaSanPham == seri.MaSanPham);
                if (chiTiet != null)
                {
                    donGiaNhap = chiTiet.DonGiaNhap;
                }
            }
            ViewData["DonGiaNhap"] = donGiaNhap;

            return View(seri);
        }

        // POST: SeriSanPham/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var seri = await _context.SeriSanPham.FindAsync(id);
            if (seri != null)
            {
                // Chốt chặn: Kiểm tra nếu Seri đã bán (2) hoặc đang bảo hành (3)
                if (seri.TrangThai == 2 || seri.TrangThai == 3)
                {
                    string msg = seri.TrangThai == 2 ? "đã bán cho khách hàng" : "đang trong quá trình bảo hành/lỗi";
                    TempData["Error"] = $"Không thể xoá mã Seri này vì máy {msg}! Vui lòng kiểm tra lại.";
                    return RedirectToAction(nameof(Index));
                }

                // Logic xóa mềm
                seri.IsDeleted = true;
                _context.Update(seri);
                await _context.SaveChangesAsync();
                await ActivityLogger.LogAsync(_context, int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1"), "Xóa", "Seri Sản Phẩm", $"Chuyển thùng rác Seri: {seri.SoSeri}");
                TempData["Success"] = $"Đã chuyển Seri {seri.SoSeri} vào thùng rác.";
            }
            return RedirectToAction(nameof(Index));
        }

        // 5. THÙNG RÁC (Hiện danh sách đã xóa)
        // GET: SeriSanPham/Trash
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
        public async Task<IActionResult> Trash(string searchString, DateTime? fromDate, DateTime? toDate)
        {
            var seriList = _context.SeriSanPham
                .Include(s => s.MaSanPhamNavigation)
                    .ThenInclude(p => p.MaDanhMucNavigation)
                .Include(s => s.MaPhieuNhapNavigation)
                    .ThenInclude(pn => pn.ChiTietPhieuNhap)
                .Include(s => s.MaPhieuXuatNavigation)
                    .ThenInclude(px => px.ChiTietPhieuXuat)
                .Where(s => s.IsDeleted == true);

            // Tìm kiếm theo chuỗi Seri
            if (!string.IsNullOrEmpty(searchString))
            {
                seriList = seriList.Where(s => s.SoSeri.Contains(searchString) ||
                                               s.MaSanPhamNavigation.TenSanPham.Contains(searchString) ||
                                               s.MaSanPhamNavigation.HangSanXuat.Contains(searchString));
            }

            // Validate ngày hợp lệ cho SQL Server
            var sqlMinDate = new DateTime(1753, 1, 1);
            var sqlMaxDate = new DateTime(9999, 12, 31);
            if (fromDate.HasValue && (fromDate.Value < sqlMinDate || fromDate.Value > sqlMaxDate))
            {
                TempData["Error"] = "Thời gian bắt đầu không hợp lệ!";
                fromDate = null;
            }
            if (toDate.HasValue && (toDate.Value < sqlMinDate || toDate.Value > sqlMaxDate))
            {
                TempData["Error"] = "Thời gian kết thúc không hợp lệ!";
                toDate = null;
            }

            // Lọc theo khoảng ngày (Mặc định theo Ngày Nhập trong Thùng Rác)
            if (fromDate.HasValue || toDate.HasValue)
            {
                if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
                {
                    TempData["Error"] = "Khoảng thời gian không hợp lệ (Từ Ngày lớn hơn Đến Ngày). Đã hủy lọc!";
                    fromDate = null;
                    toDate = null;
                }
                else
                {
                    DateTime? fDate = fromDate.HasValue ? fromDate.Value.Date : null;
                    DateTime? tDate = toDate.HasValue ? toDate.Value.Date.AddDays(1).AddTicks(-1) : null;

                    if (fDate.HasValue) seriList = seriList.Where(s => s.MaPhieuNhapNavigation != null && s.MaPhieuNhapNavigation.NgayNhap >= fDate.Value);
                    if (tDate.HasValue) seriList = seriList.Where(s => s.MaPhieuNhapNavigation != null && s.MaPhieuNhapNavigation.NgayNhap <= tDate.Value);
                }
            }

            seriList = seriList.OrderByDescending(s => s.MaPhieuNhap).ThenBy(s => s.SoSeri);

            ViewData["CurrentFilter"] = searchString;
            ViewData["FromDate"] = fromDate?.ToString("yyyy-MM-dd");
            ViewData["ToDate"] = toDate?.ToString("yyyy-MM-dd");

            return View(await seriList.ToListAsync());
        }

        // 6. KHÔI PHỤC (Hồi sinh từ thùng rác)
        // POST: SeriSanPham/Restore/5
        [HttpPost]
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var seri = await _context.SeriSanPham
                .Include(s => s.MaSanPhamNavigation)
                .Include(s => s.MaPhieuNhapNavigation)
                .FirstOrDefaultAsync(s => s.MaSeri == id);

            if (seri == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Trash));
            }

            // Chốt chặn 1: Kiểm tra Sản phẩm cha
            if (seri.MaSanPhamNavigation.IsDeleted)
            {
                TempData["Error"] = $"Không thể khôi phục mã Seri này vì Sản Phẩm '{seri.MaSanPhamNavigation.TenSanPham}' đang bị xoá. Vui lòng khôi phục Sản Phẩm [{seri.MaSanPhamNavigation.TenSanPham}] trước.";
                return RedirectToAction(nameof(Trash));
            }

            // Chốt chặn 2: Kiểm tra Phiếu nhập cha (nếu có)
            if (seri.MaPhieuNhapNavigation != null && seri.MaPhieuNhapNavigation.IsDeleted)
            {
                TempData["Error"] = $"Không thể khôi phục mã Seri này vì Phiếu Nhập mã '{seri.MaPhieuNhapNavigation.MaPhieuNhap}' đang nằm trong thùng rác. Vui lòng khôi phục Phiếu Nhập trước.";
                return RedirectToAction(nameof(Trash));
            }

            seri.IsDeleted = false;
            _context.Update(seri);
            await _context.SaveChangesAsync();
            await ActivityLogger.LogAsync(_context, int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1"), "Khôi phục", "Seri Sản Phẩm", $"Khôi phục Seri: {seri.SoSeri}");
            TempData["Success"] = $"Khôi phục Seri {seri.SoSeri} thành công.";

            return RedirectToAction(nameof(Trash));
        }

        // 7. XUẤT EXCEL DANH SÁCH SERI (Kiểm Kê Kho)
        // 9. XUẤT EXCEL DANH SÁCH SERI
        [HttpGet]
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
        public async Task<IActionResult> ExportExcel(int? trangThaiFilter, string searchString, DateTime? fromDate, DateTime? toDate)
        {
            // --- Sao chép logic lọc từ action Index ---
            var seriList = _context.SeriSanPham
                .Include(s => s.MaSanPhamNavigation)
                    .ThenInclude(sp => sp.MaDanhMucNavigation)
                .Include(s => s.MaPhieuNhapNavigation)
                    .ThenInclude(pn => pn.MaNhaCungCapNavigation)
                .Include(s => s.MaPhieuNhapNavigation)
                    .ThenInclude(pn => pn.ChiTietPhieuNhap)
                .Include(s => s.MaPhieuXuatNavigation)
                    .ThenInclude(px => px.ChiTietPhieuXuat)
                .Where(s => !s.IsDeleted);

            if (!string.IsNullOrEmpty(searchString))
            {
                seriList = seriList.Where(s => s.SoSeri.Contains(searchString) ||
                                               s.MaSanPhamNavigation.TenSanPham.Contains(searchString) ||
                                               s.MaSanPhamNavigation.HangSanXuat.Contains(searchString));
            }

            if (trangThaiFilter.HasValue)
            {
                seriList = seriList.Where(s => s.TrangThai == trangThaiFilter.Value);
            }

            // Validate ngày hợp lệ cho SQL Server
            var sqlMinDate = new DateTime(1753, 1, 1);
            var sqlMaxDate = new DateTime(9999, 12, 31);
            if (fromDate.HasValue && (fromDate.Value < sqlMinDate || fromDate.Value > sqlMaxDate)) fromDate = null;
            if (toDate.HasValue && (toDate.Value < sqlMinDate || toDate.Value > sqlMaxDate)) toDate = null;

            if (fromDate.HasValue || toDate.HasValue)
            {
                if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
                {
                    fromDate = null;
                    toDate = null;
                }
                else
                {
                    DateTime? fDate = fromDate.HasValue ? fromDate.Value.Date : null;
                    DateTime? tDate = toDate.HasValue ? toDate.Value.Date.AddDays(1).AddTicks(-1) : null;

                    if (trangThaiFilter == 1)
                    {
                        if (fDate.HasValue) seriList = seriList.Where(s => s.MaPhieuNhapNavigation != null && s.MaPhieuNhapNavigation.NgayNhap >= fDate.Value);
                        if (tDate.HasValue) seriList = seriList.Where(s => s.MaPhieuNhapNavigation != null && s.MaPhieuNhapNavigation.NgayNhap <= tDate.Value);
                    }
                    else if (trangThaiFilter == 2)
                    {
                        if (fDate.HasValue) seriList = seriList.Where(s => s.MaPhieuXuatNavigation != null && s.MaPhieuXuatNavigation.NgayXuat >= fDate.Value);
                        if (tDate.HasValue) seriList = seriList.Where(s => s.MaPhieuXuatNavigation != null && s.MaPhieuXuatNavigation.NgayXuat <= tDate.Value);
                    }
                    else if (trangThaiFilter == 3)
                    {
                        if (fDate.HasValue) seriList = seriList.Where(s =>
                            (s.MaPhieuXuat == null && s.MaPhieuNhapNavigation != null && s.MaPhieuNhapNavigation.NgayNhap >= fDate.Value) ||
                            (s.MaPhieuXuat != null && s.MaPhieuXuatNavigation != null && s.MaPhieuXuatNavigation.NgayXuat >= fDate.Value)
                        );
                        if (tDate.HasValue) seriList = seriList.Where(s =>
                            (s.MaPhieuXuat == null && s.MaPhieuNhapNavigation != null && s.MaPhieuNhapNavigation.NgayNhap <= tDate.Value) ||
                            (s.MaPhieuXuat != null && s.MaPhieuXuatNavigation != null && s.MaPhieuXuatNavigation.NgayXuat <= tDate.Value)
                        );
                    }
                    else
                    {
                        if (fDate.HasValue) seriList = seriList.Where(s => s.MaPhieuNhapNavigation != null && s.MaPhieuNhapNavigation.NgayNhap >= fDate.Value);
                        if (tDate.HasValue) seriList = seriList.Where(s => s.MaPhieuNhapNavigation != null && s.MaPhieuNhapNavigation.NgayNhap <= tDate.Value);
                    }
                }
            }

            seriList = seriList.OrderByDescending(s => s.MaPhieuNhap).ThenBy(s => s.SoSeri);
            var data = await seriList.ToListAsync();

            // --- Xuất Excel bằng ClosedXML ---
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("DanhSachSeri");

            // Tiêu đề báo cáo
            ws.Cell(1, 1).Value = "DANH SÁCH MÃ SERI - KIỂM KÊ KHO";
            ws.Range("A1:J1").Merge().Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            string trangThaiText = trangThaiFilter == 1 ? "Tồn Kho" : trangThaiFilter == 2 ? "Đã Bán" : trangThaiFilter == 3 ? "Lỗi/Bảo Hành" : "Tất cả";
            string kyBaoCao = "";
            if (fromDate.HasValue || toDate.HasValue)
                kyBaoCao = $" | Từ {fromDate?.ToString("dd/MM/yyyy") ?? "..."} đến {toDate?.ToString("dd/MM/yyyy") ?? "..."}";
            ws.Cell(2, 1).Value = $"Trạng thái: {trangThaiText}{kyBaoCao} | Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
            ws.Range("A2:J2").Merge();
            ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(2, 1).Style.Font.Italic = true;

            // Header dòng 4
            int headerRow = 4;
            string[] headers = { "STT", "Tên Sản Phẩm", "Hãng SX", "Danh Mục", "Mã Seri (SN)", "Trạng Thái", "Ngày Nhập", "Nhà Cung Cấp", "Thực tế kiểm đếm", "Ghi chú" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(headerRow, i + 1).Value = headers[i];
            }
            var headerRange = ws.Range(headerRow, 1, headerRow, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

            // Dữ liệu
            int row = headerRow + 1;
            int stt = 1;
            foreach (var seri in data)
            {
                ws.Cell(row, 1).Value = stt;
                ws.Cell(row, 2).Value = seri.MaSanPhamNavigation?.TenSanPham ?? "";
                ws.Cell(row, 3).Value = seri.MaSanPhamNavigation?.HangSanXuat ?? "";
                ws.Cell(row, 4).Value = seri.MaSanPhamNavigation?.MaDanhMucNavigation?.TenDanhMuc ?? "";
                ws.Cell(row, 5).Value = seri.SoSeri;
                ws.Cell(row, 6).Value = seri.TrangThai == 1 ? "Tồn Kho" : seri.TrangThai == 2 ? "Đã Bán" : "Lỗi/BH";
                ws.Cell(row, 7).Value = seri.MaPhieuNhapNavigation?.NgayNhap.ToString("dd/MM/yyyy") ?? "";
                ws.Cell(row, 8).Value = seri.MaPhieuNhapNavigation?.MaNhaCungCapNavigation?.TenNhaCungCap ?? "";
                // Cột 9 & 10 để trống cho thủ kho điền
                ws.Cell(row, 9).Value = "";
                ws.Cell(row, 10).Value = "";

                stt++;
                row++;
            }

            // Dòng tổng cộng
            ws.Cell(row, 1).Value = "TỔNG CỘNG:";
            ws.Range(row, 1, row, 4).Merge().Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Cell(row, 5).Value = data.Count;
            ws.Cell(row, 5).Style.Font.Bold = true;
            ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#E2EFDA");
            ws.Range(row, 1, row, headers.Length).Style.Border.TopBorder = XLBorderStyleValues.Thin;

            // Viền toàn bảng
            ws.Range(headerRow, 1, row, headers.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range(headerRow, 1, row, headers.Length).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            string fileName = $"Kiem_Ke_Seri_{DateTime.Now:dd-MM-yyyy_HHmm}.xlsx";
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
