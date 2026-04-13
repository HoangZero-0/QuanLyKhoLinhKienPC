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
using QuanLyKhoLinhKienPC.ViewModels;
using ClosedXML.Excel;
namespace QuanLyKhoLinhKienPC.Controllers
{
    // DTO (Data Transfer Object) chuyên dụng để hứng dữ liệu "Giỏ hàng" từ Front-End gửi lên
    public class ChiTietBanHang
    {
        public int MaSanPham { get; set; }
        public int SoLuong { get; set; }
        public decimal GiaTien { get; set; }
    }

    [Authorize]
    public class PhieuXuatController : Controller
    {
        private readonly QuanLyKhoLinhKienPCContext _context;

        public PhieuXuatController(QuanLyKhoLinhKienPCContext context)
        {
            _context = context;
        }

        // 1. DANH SÁCH
        // GET: PhieuXuat
        public async Task<IActionResult> Index(string searchString, int? MaNguoiDung, DateTime? fromDate, DateTime? toDate)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentMaNguoiDung"] = MaNguoiDung;
            ViewData["FromDate"] = fromDate?.ToString("yyyy-MM-dd");
            ViewData["ToDate"] = toDate?.ToString("yyyy-MM-dd");
            ViewData["MaNguoiDung"] = new SelectList(_context.NguoiDung, "MaNguoiDung", "HoTen", MaNguoiDung);

            var query = _context.PhieuXuat
                .Where(p => !p.IsDeleted)
                .Include(p => p.MaNguoiDungNavigation)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                string searchLower = searchString.ToLower().Trim();
                int parsedId = -1;
                string numberPart = searchLower.Replace("px", "").Replace("-", "").Replace("#", "").Trim();
                int.TryParse(numberPart, out parsedId);

                query = query.Where(p => p.MaPhieuXuat == parsedId ||
                                         (p.TenKhachHang != null && p.TenKhachHang.ToLower().Contains(searchLower)) ||
                                         (p.SoDienThoaiKhach != null && p.SoDienThoaiKhach.Contains(searchLower)));
            }

            if (MaNguoiDung.HasValue)
            {
                query = query.Where(p => p.MaNguoiDung == MaNguoiDung.Value);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(p => p.NgayXuat >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                var endOfDate = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(p => p.NgayXuat <= endOfDate);
            }

            return View(await query.OrderByDescending(p => p.NgayXuat).ToListAsync());
        }

        // 2. CHI TIẾT
        // GET: PhieuXuat/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            var phieuXuat = await _context.PhieuXuat
                .Include(p => p.MaNguoiDungNavigation)
                .Include(p => p.ChiTietPhieuXuat)
                    .ThenInclude(ct => ct.MaSeriNavigation)
                        .ThenInclude(s => s.MaSanPhamNavigation)
                .FirstOrDefaultAsync(m => m.MaPhieuXuat == id);
            if (phieuXuat == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            return View(phieuXuat);
        }

        // 3. TẠO MỚI
        // GET: PhieuXuat/Create
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên bán hàng")]
        public IActionResult Create()
        {
            ViewData["MaNguoiDung"] = new SelectList(_context.NguoiDung, "MaNguoiDung", "HoTen");

            // Danh sách Dropdown cho Detail (Javascript) - Kèm theo Số lượng tồn trong Kho
            var dsSanPham = _context.SanPham
                .Where(s => !s.IsDeleted)
                .Select(s => new
                {
                    MaSanPham = s.MaSanPham,
                    TenSanPham = s.TenSanPham,
                    GiaBan = s.GiaBan,
                    // Lấy danh sách Seri đang "Trong kho" (TrangThai = 1)
                    AvailableSeris = s.SeriSanPham
                        .Where(seri => seri.TrangThai == 1 && !seri.IsDeleted)
                        .Select(seri => new { seri.MaSeri, seri.SoSeri })
                        .ToList()
                }).ToList();

            ViewData["SanPhamList"] = dsSanPham;

            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên bán hàng")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PhieuXuatVM model)
        {
            if (ModelState.IsValid)
            {
                if (model.Items == null || !model.Items.Any(i => i.SelectedSeriIds != null && i.SelectedSeriIds.Any()))
                {
                    ModelState.AddModelError("", "Vui lòng chọn ít nhất một mã Seri để xuất kho!");
                }
                else
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        var phieuXuat = new PhieuXuat
                        {
                            TenKhachHang = model.TenKhachHang,
                            SoDienThoaiKhach = model.SoDienThoaiKhach,
                            NgayXuat = DateTime.Now,
                            IsDeleted = false,
                            // Lấy mã người dùng từ Claims (Account/Login)
                            MaNguoiDung = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1")
                        };

                        // Tính tổng tiền dựa trên đơn giá và số lượng Seri chọn
                        phieuXuat.TongTien = model.Items.Sum(i => (i.SelectedSeriIds?.Count ?? 0) * i.DonGiaXuat);

                        _context.PhieuXuat.Add(phieuXuat);
                        await _context.SaveChangesAsync();

                        var lstChiTietXuat = new List<ChiTietPhieuXuat>();
                        var lstSeriUpdate = new List<SeriSanPham>();

                        foreach (var item in model.Items)
                        {
                            if (item.SelectedSeriIds == null || !item.SelectedSeriIds.Any()) continue;

                            // Lấy danh sách Seri thực tế từ DB để kiểm tra trạng thái
                            var serisFromDb = await _context.SeriSanPham
                                .Where(s => item.SelectedSeriIds.Contains(s.MaSeri))
                                .ToListAsync();

                            foreach (var sId in item.SelectedSeriIds)
                            {
                                var seriObj = serisFromDb.FirstOrDefault(s => s.MaSeri == sId);
                                if (seriObj == null || seriObj.TrangThai != 1 || seriObj.IsDeleted)
                                {
                                    throw new Exception($"Mã Seri {sId} không khả dụng hoặc đã bị bán/xóa!");
                                }

                                // Cập nhật trạng thái Seri
                                seriObj.TrangThai = 2; // Đã bán
                                seriObj.MaPhieuXuat = phieuXuat.MaPhieuXuat;
                                lstSeriUpdate.Add(seriObj);

                                // Tạo chi tiết phiếu xuất
                                lstChiTietXuat.Add(new ChiTietPhieuXuat
                                {
                                    MaPhieuXuat = phieuXuat.MaPhieuXuat,
                                    MaSeri = sId,
                                    GiaTien = item.DonGiaXuat
                                });
                            }
                        }

                        _context.ChiTietPhieuXuat.AddRange(lstChiTietXuat);
                        _context.SeriSanPham.UpdateRange(lstSeriUpdate);

                        await _context.SaveChangesAsync();

                        await ActivityLogger.LogAsync(_context, phieuXuat.MaNguoiDung, "Thêm mới", "Phiếu Xuất", $"Lập Phiếu Xuất kho #{phieuXuat.MaPhieuXuat} cho khách {phieuXuat.TenKhachHang} với {lstSeriUpdate.Count} mã Seri.");

                        await transaction.CommitAsync();

                        TempData["Success"] = $"Lập Phiếu Xuất thành công! Tổng cộng {lstSeriUpdate.Count} mã Seri đã xuất kho.";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError("", "Lỗi nghiệp vụ: " + ex.Message);
                    }
                }
            }

            // Load lại dữ liệu nếu lỗi
            ViewData["SanPhamList"] = _context.SanPham
                .Where(s => !s.IsDeleted)
                .Select(s => new
                {
                    MaSanPham = s.MaSanPham,
                    TenSanPham = s.TenSanPham,
                    GiaBan = s.GiaBan,
                    AvailableSeris = s.SeriSanPham
                        .Where(seri => seri.TrangThai == 1 && !seri.IsDeleted)
                        .Select(seri => new { seri.MaSeri, seri.SoSeri })
                        .ToList()
                }).ToList();

            return View(model);
        }

        // 4. CHỈNH SỬA
        // GET: PhieuXuat/Edit/5
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên bán hàng")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            var phieuXuat = await _context.PhieuXuat
                .Include(p => p.MaNguoiDungNavigation)
                .FirstOrDefaultAsync(p => p.MaPhieuXuat == id);
            if (phieuXuat == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }
            return View(phieuXuat);
        }

        // POST: PhieuXuat/Edit/5
        [HttpPost]
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên bán hàng")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaPhieuXuat,TenKhachHang,SoDienThoaiKhach")] PhieuXuat phieuXuat)
        {
            if (id != phieuXuat.MaPhieuXuat)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var pxFromDb = await _context.PhieuXuat.FirstOrDefaultAsync(p => p.MaPhieuXuat == id);
                    if (pxFromDb == null)
                    {
                        TempData["Error"] = "Không tìm thấy Phiếu Xuất!";
                        return RedirectToAction(nameof(Index));
                    }

                    // Chỉ cho phép cập nhật thông tin khách hàng, tránh can thiệp Ngày Xuất, Người Lập, Tổng Tiền
                    pxFromDb.TenKhachHang = phieuXuat.TenKhachHang;
                    pxFromDb.SoDienThoaiKhach = phieuXuat.SoDienThoaiKhach;

                    _context.Update(pxFromDb);
                    await _context.SaveChangesAsync();

                    await ActivityLogger.LogAsync(_context, int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1"), "Cập nhật", "Phiếu Xuất", $"Cập nhật thông tin khách hàng tại phiếu PX-{phieuXuat.MaPhieuXuat}");

                    await transaction.CommitAsync();
                    TempData["Success"] = "Cập nhật thông tin Phiếu Xuất thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = "Lỗi khi cập nhật phiếu xuất: " + ex.Message;
                }
            }
            // Nếu lỗi trả về View, cần truyền đủ thuộc tính ẩn
            var fallbackPx = await _context.PhieuXuat
                .Include(p => p.MaNguoiDungNavigation)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.MaPhieuXuat == phieuXuat.MaPhieuXuat);
            return View(fallbackPx ?? phieuXuat);
        }

        // 5. XÓA MỀM (Chuyển vào thùng rác)
        // GET: PhieuXuat/Delete/5
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên bán hàng")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            var phieuXuat = await _context.PhieuXuat
                .Include(p => p.MaNguoiDungNavigation)
                .FirstOrDefaultAsync(m => m.MaPhieuXuat == id);
            if (phieuXuat == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            return View(phieuXuat);
        }

        // POST: PhieuXuat/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên bán hàng")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var phieuXuat = await _context.PhieuXuat.FindAsync(id);
            if (phieuXuat != null)
            {
                // Chốt chặn 1: Kiểm tra nếu có mã Seri trong hóa đơn đã bị xóa lẻ thủ công trước đó
                bool hasDeletedSerials = await _context.ChiTietPhieuXuat
                    .Include(ct => ct.MaSeriNavigation)
                    .AnyAsync(ct => ct.MaPhieuXuat == id && ct.MaSeriNavigation.IsDeleted);

                if (hasDeletedSerials)
                {
                    TempData["Error"] = "Không thể hủy Phiếu Xuất này vì có một số mã Seri trong đơn đã bị xóa lẻ khỏi hệ thống trước đó! Vui lòng khôi phục mã Seri trước khi hủy Phiếu Xuất.";
                    return RedirectToAction(nameof(Index));
                }

                // Chốt chặn 2: Kiểm tra nếu có mã Seri đang bảo hành
                bool hasWarrantySerials = await _context.ChiTietPhieuXuat
                    .Include(ct => ct.MaSeriNavigation)
                    .AnyAsync(ct => ct.MaPhieuXuat == id && ct.MaSeriNavigation.TrangThai == 3);

                if (hasWarrantySerials)
                {
                    TempData["Error"] = "Không thể hủy Phiếu Xuất này vì có Sản phẩm đang trong quá trình bảo hành/xử lý (Trạng thái 3). Vui lòng hoàn tất bảo hành trước khi hủy!";
                    return RedirectToAction(nameof(Index));
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 1. Soft delete phiếu xuất
                    phieuXuat.IsDeleted = true;
                    _context.Update(phieuXuat);

                    // 2. Trả lại Seri về kho (TrangThai = 1, thoát khỏi MaPhieuXuat)
                    var seriXuatList = await _context.SeriSanPham
                        .Where(s => s.MaPhieuXuat == id)
                        .ToListAsync();

                    foreach (var seri in seriXuatList)
                    {
                        seri.TrangThai = 1; // Trả lại kho
                        seri.MaPhieuXuat = null;
                    }
                    _context.UpdateRange(seriXuatList);

                    await _context.SaveChangesAsync();
                    await ActivityLogger.LogAsync(_context, int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1"), "Hủy phiếu", "Phiếu Xuất", $"Hủy lệnh xuất kho #PX-{phieuXuat.MaPhieuXuat} và tự động rút Seri về kho.");
                    await transaction.CommitAsync();

                    TempData["Success"] = "Đã chuyển Phiếu Xuất vào lịch sử hủy và hoàn trả mã Seri nguyên vẹn về kho hàng.";
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = "Lỗi khi xóa hóa đơn: " + ex.Message;
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // 6. THÙNG RÁC (Hiện danh sách đã xóa)
        // GET: PhieuXuat/Trash
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên bán hàng")]
        public async Task<IActionResult> Trash(string searchString, int? MaNguoiDung, DateTime? fromDate, DateTime? toDate)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["FromDate"] = fromDate?.ToString("yyyy-MM-dd");
            ViewData["ToDate"] = toDate?.ToString("yyyy-MM-dd");
            ViewData["MaNguoiDung"] = new SelectList(_context.NguoiDung.Where(u => !u.IsDeleted), "MaNguoiDung", "HoTen", MaNguoiDung);

            var query = _context.PhieuXuat
                .Where(p => p.IsDeleted)
                .Include(p => p.MaNguoiDungNavigation)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                string searchLower = searchString.ToLower().Trim();
                int parsedId = -1;
                string numberPart = searchLower.Replace("px", "").Replace("-", "").Replace("#", "").Trim();
                int.TryParse(numberPart, out parsedId);

                query = query.Where(p => p.MaPhieuXuat == parsedId ||
                                         (p.TenKhachHang != null && p.TenKhachHang.ToLower().Contains(searchLower)) ||
                                         (p.SoDienThoaiKhach != null && p.SoDienThoaiKhach.Contains(searchLower)));
            }

            if (MaNguoiDung.HasValue)
            {
                query = query.Where(p => p.MaNguoiDung == MaNguoiDung.Value);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(p => p.NgayXuat >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                var endOfDate = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(p => p.NgayXuat <= endOfDate);
            }

            return View(await query.OrderByDescending(p => p.NgayXuat).ToListAsync());
        }

        // 7. KHÔI PHỤC (Hồi sinh từ thùng rác)
        // POST: PhieuXuat/Restore
        [HttpPost]
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên bán hàng")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var phieuXuat = await _context.PhieuXuat
                .Include(p => p.MaNguoiDungNavigation)
                .FirstOrDefaultAsync(p => p.MaPhieuXuat == id);

            if (phieuXuat == null)
            {
                TempData["Error"] = "Không tìm thấy Phiếu Xuất!";
                return RedirectToAction(nameof(Trash));
            }

            // Chốt chặn: Kiểm tra Người lập phiếu (Nhân viên)
            if (phieuXuat.MaNguoiDungNavigation.IsDeleted)
            {
                TempData["Error"] = $"Không thể khôi phục Phiếu Xuất này vì Nhân Viên lập phiếu '{phieuXuat.MaNguoiDungNavigation.HoTen}' đang bị khoá. Vui lòng mở khoá Nhân Viên trước.";
                return RedirectToAction(nameof(Trash));
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Lấy các ChiTietPhieuXuat để biết những Seri nào cần lấy lại
                var chiTietList = await _context.ChiTietPhieuXuat
                    .Where(ct => ct.MaPhieuXuat == id)
                    .Select(ct => ct.MaSeri)
                    .ToListAsync();

                var seriList = await _context.SeriSanPham
                    .Where(s => chiTietList.Contains(s.MaSeri))
                    .ToListAsync();

                // Kiểm tra xem có Seri nào đã bị đem bán cho hóa đơn khác chưa
                if (seriList.Any(s => s.TrangThai != 1))
                {
                    TempData["Error"] = "Không thể khôi phục hóa đơn này vì một số Seri thuộc hóa đơn đã được xuất bán lại cho khách hàng khác!";
                    return RedirectToAction(nameof(Trash));
                }

                // Nếu tất cả seri đều trong kho (TrangThai = 1)
                foreach (var seri in seriList)
                {
                    seri.TrangThai = 2; // Đặt lại thành Đã bán
                    seri.MaPhieuXuat = id;
                }
                _context.UpdateRange(seriList);

                phieuXuat.IsDeleted = false;
                _context.Update(phieuXuat);

                await _context.SaveChangesAsync();
                await ActivityLogger.LogAsync(_context, int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1"), "Khôi phục", "Phiếu Xuất", $"Khôi phục phiếu PX-{phieuXuat.MaPhieuXuat}");
                await transaction.CommitAsync();

                TempData["Success"] = "Đã khôi phục Phiếu Xuất và rút lại mã Seri thành công.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Lỗi khi khôi phục: " + ex.Message;
            }

            return RedirectToAction(nameof(Trash));
        }

        private bool PhieuXuatExists(int id)
        {
            return _context.PhieuXuat.Any(e => e.MaPhieuXuat == id);
        }

        // 10. XUẤT EXCEL BÁO CÁO DOANH THU & LỢI NHUẬN
        // 8. Xuất Excel Báo Cáo Doanh Thu
        [HttpGet]
        [Authorize(Roles = "Quản trị viên,Admin")]
        public async Task<IActionResult> ExportDoanhThu(DateTime? fromDate, DateTime? toDate)
        {
            // Validate ngày hợp lệ cho SQL Server
            var sqlMinDate = new DateTime(1753, 1, 1);
            var sqlMaxDate = new DateTime(9999, 12, 31);

            if (fromDate.HasValue && (fromDate.Value < sqlMinDate || fromDate.Value > sqlMaxDate)) fromDate = null;
            if (toDate.HasValue && (toDate.Value < sqlMinDate || toDate.Value > sqlMaxDate)) toDate = null;

            if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
            {
                fromDate = null;
                toDate = null;
            }

            var query = _context.PhieuXuat
                .Where(p => !p.IsDeleted)
                .Include(p => p.MaNguoiDungNavigation)
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(p => p.NgayXuat >= fromDate.Value.Date);
            if (toDate.HasValue)
                query = query.Where(p => p.NgayXuat <= toDate.Value.Date.AddDays(1).AddTicks(-1));

            var data = await query.OrderByDescending(p => p.NgayXuat).ToListAsync();

            // Lấy toàn bộ Seri đã bán thuộc các phiếu xuất này để tính Tiền Vốn
            var maPhieuXuats = data.Select(p => (int?)p.MaPhieuXuat).ToList();
            var seriDaBan = await _context.SeriSanPham
                .Where(s => s.MaPhieuXuat != null && maPhieuXuats.Contains(s.MaPhieuXuat))
                .Include(s => s.MaPhieuNhapNavigation)
                    .ThenInclude(pn => pn.ChiTietPhieuNhap)
                .ToListAsync();

            // Tính Tiền Vốn cho từng phiếu xuất
            var tienVonDict = new Dictionary<int, decimal>();
            foreach (var px in data)
            {
                var seriCuaPhieu = seriDaBan.Where(s => s.MaPhieuXuat == px.MaPhieuXuat).ToList();
                decimal tienVon = 0;
                foreach (var seri in seriCuaPhieu)
                {
                    // Tìm đơn giá nhập của seri này từ ChiTietPhieuNhap
                    var chiTietNhap = seri.MaPhieuNhapNavigation?.ChiTietPhieuNhap
                        ?.FirstOrDefault(ct => ct.MaSanPham == seri.MaSanPham);
                    if (chiTietNhap != null)
                        tienVon += chiTietNhap.DonGiaNhap;
                }
                tienVonDict[px.MaPhieuXuat] = tienVon;
            }

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("DoanhThu_LoiNhuan");

            // Tiêu đề
            ws.Cell(1, 1).Value = "BÁO CÁO DOANH THU & LỢI NHUẬN";
            ws.Range("A1:I1").Merge().Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            string kyBaoCao = "Tất cả";
            if (fromDate.HasValue || toDate.HasValue)
                kyBaoCao = $"Từ {fromDate?.ToString("dd/MM/yyyy") ?? "..."} đến {toDate?.ToString("dd/MM/yyyy") ?? "..."}";
            ws.Cell(2, 1).Value = $"Kỳ báo cáo: {kyBaoCao} | Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
            ws.Range("A2:I2").Merge();
            ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(2, 1).Style.Font.Italic = true;

            // Header dòng 4
            int headerRow = 4;
            string[] headers = { "STT", "Mã Phiếu", "Ngày Xuất", "Tên Khách Hàng", "SĐT Khách", "Người Lập Phiếu", "Doanh Thu", "Tiền Vốn", "Lợi Nhuận" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(headerRow, i + 1).Value = headers[i];
            }
            var headerRange = ws.Range(headerRow, 1, headerRow, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Dữ liệu
            int row = headerRow + 1;
            int stt = 1;
            decimal tongDoanhThu = 0, tongTienVon = 0, tongLoiNhuan = 0;
            foreach (var px in data)
            {
                decimal doanhThu = px.TongTien;
                decimal tienVon = tienVonDict.GetValueOrDefault(px.MaPhieuXuat, 0);
                decimal loiNhuan = doanhThu - tienVon;

                ws.Cell(row, 1).Value = stt;
                ws.Cell(row, 2).Value = $"PX-{px.MaPhieuXuat:D5}";
                ws.Cell(row, 3).Value = px.NgayXuat.ToString("dd/MM/yyyy HH:mm");
                ws.Cell(row, 4).Value = px.TenKhachHang ?? "";
                ws.Cell(row, 5).Value = px.SoDienThoaiKhach ?? "";
                ws.Cell(row, 6).Value = px.MaNguoiDungNavigation?.HoTen ?? "";
                ws.Cell(row, 7).Value = doanhThu;
                ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0";
                ws.Cell(row, 8).Value = tienVon;
                ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0";
                ws.Cell(row, 9).Value = loiNhuan;
                ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0";

                // Highlight lỗ (lợi nhuận âm)
                if (loiNhuan < 0)
                    ws.Range(row, 1, row, headers.Length).Style.Font.FontColor = XLColor.Red;

                tongDoanhThu += doanhThu;
                tongTienVon += tienVon;
                tongLoiNhuan += loiNhuan;
                stt++;
                row++;
            }

            // Dòng TỔNG CỘNG
            ws.Cell(row, 1).Value = "TỔNG CỘNG:";
            ws.Range(row, 1, row, 6).Merge().Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Cell(row, 7).Value = tongDoanhThu;
            ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 8).Value = tongTienVon;
            ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 9).Value = tongLoiNhuan;
            ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0";
            ws.Range(row, 7, row, 9).Style.Font.Bold = true;
            ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#E2EFDA");
            ws.Range(row, 1, row, headers.Length).Style.Border.TopBorder = XLBorderStyleValues.Thin;

            // Dòng tổng số hóa đơn
            row++;
            ws.Cell(row, 1).Value = $"Tổng số hóa đơn: {data.Count}";
            ws.Range(row, 1, row, headers.Length).Merge().Style.Font.Italic = true;

            // Viền bảng
            ws.Range(headerRow, 1, row - 1, headers.Length).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range(headerRow, 1, row - 1, headers.Length).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            string fileName = $"Doanh_Thu_Loi_Nhuan_{DateTime.Now:dd-MM-yyyy_HHmm}.xlsx";
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
