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
                TempData["Error"] = "Không tìm thấy lỗi seri!";
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
                    TempData["Success"] = $"Seri: {seri.SoSeri} đã hoàn tất bảo hành và bàn giao lại cho Khách hàng.";
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
                        TempData["Success"] = $"Đã tiếp nhận bảo hành Seri: {seri.SoSeri} (Còn hạn bảo hành đến {ngayHetHan:dd/MM/yyyy}).";
                    }
                    else
                    {
                        seri.TrangThai = 3;
                        TempData["Warning"] = $"Seri: {seri.SoSeri} ĐÃ HẾT HẠN BẢO HÀNH từ {ngayHetHan:dd/MM/yyyy}. Đã nhận phiếu sửa chữa dịch vụ tính phí.";
                    }
                }
            }

            _context.Update(seri);
            await _context.SaveChangesAsync();

            // Trích xuất filter hiện tại để quay lại đúng trạng thái đang ở
            string referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer))
            {
                return Redirect(referer);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
