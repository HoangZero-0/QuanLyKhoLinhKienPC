using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyKhoLinhKienPC.Models;
using Microsoft.AspNetCore.Authorization;
using QuanLyKhoLinhKienPC.Helpers;
using System.Security.Claims;
using QuanLyKhoLinhKienPC.ViewModels;

namespace QuanLyKhoLinhKienPC.Controllers
{
    [Authorize]
    public class PhieuNhapController : Controller
    {
        private readonly QuanLyKhoLinhKienPCContext _context;

        public PhieuNhapController(QuanLyKhoLinhKienPCContext context)
        {
            _context = context;
        }

        // 1. DANH SÁCH
        // GET: PhieuNhap
        public async Task<IActionResult> Index(string searchId, int? MaNhaCungCap, int? MaNguoiDung, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.PhieuNhap
                .Where(p => !p.IsDeleted)
                .Include(p => p.MaNguoiDungNavigation)
                .Include(p => p.MaNhaCungCapNavigation)
                .Include(p => p.ChiTietPhieuNhap)
                .AsQueryable();

            // Lọc theo Mã phiếu (Nhập trên thanh tìm kiếm)
            if (!string.IsNullOrEmpty(searchId))
            {
                // Hỗ trợ cả định dạng "PN-00001" hoặc chỉ số "1"
                string idStr = searchId.Replace("PN-", "").Replace("#", "").Trim();
                if (int.TryParse(idStr, out int id))
                {
                    query = query.Where(p => p.MaPhieuNhap == id);
                }
                else
                {
                    // Nếu không phải số, tìm theo Ghi chú
                    query = query.Where(p => p.GhiChu.Contains(searchId));
                }
            }

            // Lọc theo Nhà cung cấp (Dropdown)
            if (MaNhaCungCap.HasValue)
            {
                query = query.Where(p => p.MaNhaCungCap == MaNhaCungCap);
            }

            // Lọc theo Người nhập (Dropdown)
            if (MaNguoiDung.HasValue)
            {
                query = query.Where(p => p.MaNguoiDung == MaNguoiDung);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(p => p.NgayNhap >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                var searchToDate = toDate.Value.AddDays(1);
                query = query.Where(p => p.NgayNhap < searchToDate);
            }

            // Chuẩn bị SelectList cho Dropdown lọc
            ViewBag.MaNhaCungCap = new SelectList(_context.NhaCungCap.Where(n => !n.IsDeleted), "MaNhaCungCap", "TenNhaCungCap", MaNhaCungCap);
            ViewBag.MaNguoiDung = new SelectList(_context.NguoiDung, "MaNguoiDung", "HoTen", MaNguoiDung);

            // Lưu trạng thái tìm kiếm
            ViewBag.CurrentSearchId = searchId;
            ViewBag.CurrentMaNhaCungCap = MaNhaCungCap;
            ViewBag.CurrentMaNguoiDung = MaNguoiDung;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            var data = await query.OrderByDescending(p => p.NgayNhap).ToListAsync();
            return View(data);
        }

        // 2. CHI TIẾT
        // GET: PhieuNhap/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            var phieuNhap = await _context.PhieuNhap
                .Include(p => p.MaNguoiDungNavigation)
                .Include(p => p.MaNhaCungCapNavigation)
                .Include(p => p.ChiTietPhieuNhap)
                    .ThenInclude(ct => ct.MaSanPhamNavigation)
                .Include(p => p.SeriSanPham) // Thêm dòng này để lấy danh sách mã Seri
                .FirstOrDefaultAsync(m => m.MaPhieuNhap == id);
            if (phieuNhap == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            return View(phieuNhap);
        }

        // 3. TẠO MỚI
        // GET: PhieuNhap/Create
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
        public IActionResult Create()
        {
            // Danh sách Dropdown cho Master
            ViewData["MaNguoiDung"] = new SelectList(_context.NguoiDung, "MaNguoiDung", "HoTen");
            ViewData["MaNhaCungCap"] = new SelectList(_context.NhaCungCap.Where(n => !n.IsDeleted), "MaNhaCungCap", "TenNhaCungCap");

            // Danh sách Dropdown cho Detail (Javascript)
            ViewBag.SanPhamList = _context.SanPham.Where(s => !s.IsDeleted).Select(s => new
            {
                MaSanPham = s.MaSanPham,
                TenSanPham = s.TenSanPham
            }).ToList();

            return View();
        }

        // POST: PhieuNhap/Create
        [HttpPost]
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PhieuNhapVM model)
        {
            if (model.Items == null || !model.Items.Any())
            {
                ModelState.AddModelError("", "Vui lòng thêm ít nhất 1 linh kiện vào Phiếu Nhập!");
            }

            if (ModelState.IsValid)
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var phieuNhap = new PhieuNhap
                        {
                            MaNhaCungCap = model.MaNhaCungCap,
                            GhiChu = model.GhiChu,
                            NgayNhap = DateTime.Now,
                            MaNguoiDung = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1"),
                            IsDeleted = false
                        };

                        _context.PhieuNhap.Add(phieuNhap);
                        await _context.SaveChangesAsync(); // Để lấy MaPhieuNhap

                        // --- BƯỚC 1: THU THẬP VÀ KIỂM TRA TRÙNG TRONG LÔ (BATCH-SIDE) ---
                        var allIncomingSeris = model.Items!
                            .SelectMany(item => (item.RawSeris ?? "").Replace("|||", "\n")
                                .Split(new[] { '\n', '\r', ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim().ToUpper()) // Chuẩn hóa: Trim và chữ Hoa
                                .Where(s => !string.IsNullOrEmpty(s)))
                            .ToList();

                        var batchDuplicates = allIncomingSeris
                            .GroupBy(s => s, StringComparer.OrdinalIgnoreCase)
                            .Where(g => g.Count() > 1)
                            .Select(g => g.Key)
                            .ToList();

                        if (batchDuplicates.Any())
                        {
                            throw new Exception($"Phát hiện {batchDuplicates.Count} mã Seri bị nhập trùng lặp trong chính phiếu này: {string.Join(", ", batchDuplicates)}");
                        }

                        // --- BƯỚC 2: KIỂM TRA TRÙNG VỚI DATABASE (DB-SIDE) ---
                        var dbDuplicates = await _context.SeriSanPham
                            .Where(s => allIncomingSeris.Contains(s.SoSeri) && !s.IsDeleted)
                            .Select(s => s.SoSeri)
                            .ToListAsync();

                        if (dbDuplicates.Any())
                        {
                            throw new Exception($"Phát hiện {dbDuplicates.Count} mã máy đã tồn tại trong hệ thống: {string.Join(", ", dbDuplicates)}");
                        }

                        // --- BƯỚC 3: XỬ LÝ LƯU DỮ LIỆU ---
                        var lstChiTiet = new List<ChiTietPhieuNhap>();
                        var lstSeriMoi = new List<SeriSanPham>();

                        foreach (var item in model.Items!)
                        {
                            var raw = (item.RawSeris ?? "").Replace("|||", "\n");
                            var seris = raw.Split(new[] { '\n', '\r', ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                                           .Select(s => s.Trim().ToUpper())
                                           .Where(s => !string.IsNullOrEmpty(s))
                                           .Distinct()
                                           .ToList();

                            if (seris.Count == 0) continue;

                            var chiTiet = new ChiTietPhieuNhap
                            {
                                MaPhieuNhap = phieuNhap.MaPhieuNhap,
                                MaSanPham = item.MaSanPham,
                                SoLuong = seris.Count,
                                DonGiaNhap = item.DonGiaNhap
                            };
                            lstChiTiet.Add(chiTiet);

                            foreach (var s in seris)
                            {
                                lstSeriMoi.Add(new SeriSanPham
                                {
                                    SoSeri = s,
                                    TrangThai = (int)SeriStatus.TrongKho,
                                    MaSanPham = item.MaSanPham,
                                    MaPhieuNhap = phieuNhap.MaPhieuNhap,
                                    IsDeleted = false
                                });
                            }
                        }

                        // Tính lại tổng tiền phiếu
                        phieuNhap.TongTien = lstChiTiet.Sum(c => c.SoLuong * c.DonGiaNhap);

                        _context.ChiTietPhieuNhap.AddRange(lstChiTiet);
                        _context.SeriSanPham.AddRange(lstSeriMoi);

                        await _context.SaveChangesAsync();

                        await ActivityLogger.LogAsync(_context, phieuNhap.MaNguoiDung, "Thêm mới", "Phiếu Nhập", $"Lập phiếu nhập kho #{phieuNhap.MaPhieuNhap} với {lstSeriMoi.Count} mã máy.");

                        await transaction.CommitAsync();

                        TempData["Success"] = $"Lập phiếu nhập thành công! Đã nhập {lstSeriMoi.Count} mã máy vào kho.";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError("", "Lỗi nghiệp vụ: " + ex.Message);
                    }
                }
            }

            // Nếu lỗi, load lại dữ liệu dropdown
            ViewData["MaNhaCungCap"] = new SelectList(_context.NhaCungCap.Where(n => !n.IsDeleted), "MaNhaCungCap", "TenNhaCungCap", model.MaNhaCungCap);
            ViewBag.SanPhamList = _context.SanPham.Where(s => !s.IsDeleted).Select(s => new { s.MaSanPham, s.TenSanPham }).ToList();

            return View(model);
        }

        // Action nhận file Excel và trả về JSON để điền vào Grid
        [HttpPost]
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0) return Json(new { success = false, message = "Chưa chọn file!" });

            try
            {
                using var stream = file.OpenReadStream();
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Bỏ qua header

                var result = new List<object>();
                var errorRows = new List<string>();

                foreach (var row in rows)
                {
                    string maSP = "";
                    decimal donGia = 0;
                    string rawSeris = "";

                    try
                    {
                        maSP = row.Cell(1).GetValue<string>()?.Trim() ?? "";
                        var donGiaObj = row.Cell(2).Value;
                        if (!decimal.TryParse(donGiaObj.ToString(), out donGia)) donGia = 0;
                        rawSeris = row.Cell(3).GetValue<string>() ?? "";
                    }
                    catch { continue; }

                    if (string.IsNullOrEmpty(maSP)) continue;

                    var sp = await _context.SanPham.FirstOrDefaultAsync(s => s.MaSanPham.ToString() == maSP || s.TenSanPham == maSP);
                    if (sp != null)
                    {
                        result.Add(new
                        {
                            MaSanPham = sp.MaSanPham,
                            TenSanPham = sp.TenSanPham,
                            DonGiaNhap = donGia,
                            RawSeris = string.Join("\n", rawSeris.Split(new[] { ',', ';', '|', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                                                .Select(s => s.Trim().ToUpper())
                                                                .Where(s => !string.IsNullOrEmpty(s)))
                        });
                    }
                    else
                    {
                        errorRows.Add($"Dòng {row.RowNumber()}: Không tìm thấy sản phẩm '{maSP}'");
                    }
                }

                return Json(new
                {
                    success = true,
                    data = result,
                    warnings = errorRows.Any() ? string.Join("<br/>", errorRows) : null
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi đọc file: " + ex.Message });
            }
        }

        // 4. CHỈNH SỬA
        // GET: PhieuNhap/Edit/5
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            var phieuNhap = await _context.PhieuNhap
                .Include(p => p.MaNguoiDungNavigation)
                .Include(p => p.MaNhaCungCapNavigation)
                .FirstOrDefaultAsync(m => m.MaPhieuNhap == id);
            if (phieuNhap == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaNguoiDung"] = new SelectList(_context.NguoiDung, "MaNguoiDung", "MatKhau", phieuNhap.MaNguoiDung);
            ViewData["MaNhaCungCap"] = new SelectList(_context.NhaCungCap, "MaNhaCungCap", "TenNhaCungCap", phieuNhap.MaNhaCungCap);
            return View(phieuNhap);
        }

        // POST: PhieuNhap/Edit/5
        [HttpPost]
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaPhieuNhap,NgayNhap,TongTien,GhiChu,MaNhaCungCap,MaNguoiDung,IsDeleted")] PhieuNhap phieuNhap)
        {
            if (id != phieuNhap.MaPhieuNhap)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(phieuNhap);
                    await _context.SaveChangesAsync();
                    await ActivityLogger.LogAsync(_context, int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1"), "Cập nhật", "Phiếu Nhập", $"Cập nhật thông tin phiếu PN-{phieuNhap.MaPhieuNhap}");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PhieuNhapExists(phieuNhap.MaPhieuNhap))
                    {
                        TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaNguoiDung"] = new SelectList(_context.NguoiDung, "MaNguoiDung", "MatKhau", phieuNhap.MaNguoiDung);
            ViewData["MaNhaCungCap"] = new SelectList(_context.NhaCungCap, "MaNhaCungCap", "TenNhaCungCap", phieuNhap.MaNhaCungCap);
            return View(phieuNhap);
        }

        // 5. XÓA MỀM (Chuyển vào thùng rác)
        // GET: PhieuNhap/Delete/5
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            var phieuNhap = await _context.PhieuNhap
                .Include(p => p.MaNguoiDungNavigation)
                .Include(p => p.MaNhaCungCapNavigation)
                .FirstOrDefaultAsync(m => m.MaPhieuNhap == id);
            if (phieuNhap == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            return View(phieuNhap);
        }

        // POST: PhieuNhap/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var phieuNhap = await _context.PhieuNhap.FindAsync(id);
            if (phieuNhap != null)
            {
                // Chốt chặn: Kiểm tra nếu có bất kỳ mã máy (Seri) trong phiếu đã bán (2) hoặc báo lỗi (3)
                bool hasSoldOrDefective = await _context.SeriSanPham
                    .AnyAsync(s => s.MaPhieuNhap == id && !s.IsDeleted && (s.TrangThai == 2 || s.TrangThai == 3));

                if (hasSoldOrDefective)
                {
                    TempData["Error"] = "Không thể xoá Phiếu Nhập này vì có sản phẩm trong phiếu đã được Xuất bán hoặc đang trong quá trình bảo hành/lỗi!";
                    return RedirectToAction(nameof(Index));
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 1. Soft delete phiếu nhập
                    phieuNhap.IsDeleted = true;
                    _context.Update(phieuNhap);
                    await _context.SaveChangesAsync();
                    await ActivityLogger.LogAsync(_context, int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1"), "Xóa", "Phiếu Nhập", $"Chuyển thùng rác phiếu PN-{phieuNhap.MaPhieuNhap}");
                    TempData["Success"] = "Đã chuyển Phiếu Nhập vào thùng rác.";

                    // 2. Soft delete các SeriSanPham thuộc Phiếu nhập này
                    var seriList = await _context.SeriSanPham
                        .Where(s => s.MaPhieuNhap == id && !s.IsDeleted)
                        .ToListAsync();

                    foreach (var seri in seriList)
                    {
                        seri.IsDeleted = true;
                    }
                    _context.UpdateRange(seriList);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["Success"] = "Đã chuyển Phiếu Nhập và các Seri liên quan vào thùng rác.";
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = "Lỗi khi xóa Phiếu Nhập: " + ex.Message;
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // 6. THÙNG RÁC (Hiện danh sách đã xóa)
        // GET: PhieuNhap/Trash
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
        public async Task<IActionResult> Trash(string searchString, int? MaNhaCungCap, int? MaNguoiDung)
        {
            var query = _context.PhieuNhap
                .Where(p => p.IsDeleted)
                .Include(p => p.MaNguoiDungNavigation)
                .Include(p => p.MaNhaCungCapNavigation)
                .AsQueryable();

            // Tìm kiếm nhanh (searchId hoặc Ghi chú)
            if (!string.IsNullOrEmpty(searchString))
            {
                string idStr = searchString.Replace("PN-", "").Replace("#", "").Trim();
                if (int.TryParse(idStr, out int id))
                {
                    query = query.Where(p => p.MaPhieuNhap == id);
                }
                else
                {
                    query = query.Where(p => p.GhiChu.Contains(searchString));
                }
            }

            // Lọc theo NCC
            if (MaNhaCungCap.HasValue)
            {
                query = query.Where(p => p.MaNhaCungCap == MaNhaCungCap);
            }

            // Lọc theo Người dùng
            if (MaNguoiDung.HasValue)
            {
                query = query.Where(p => p.MaNguoiDung == MaNguoiDung);
            }

            ViewBag.MaNhaCungCap = new SelectList(_context.NhaCungCap.Where(n => !n.IsDeleted), "MaNhaCungCap", "TenNhaCungCap", MaNhaCungCap);
            ViewBag.MaNguoiDung = new SelectList(_context.NguoiDung.Where(u => !u.IsDeleted), "MaNguoiDung", "HoTen", MaNguoiDung);
            ViewData["CurrentFilter"] = searchString;

            return View(await query.OrderByDescending(p => p.NgayNhap).ToListAsync());
        }

        // 7. KHÔI PHỤC (Hồi sinh từ thùng rác)
        // POST: PhieuNhap/Restore
        [HttpPost]
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var phieuNhap = await _context.PhieuNhap
                .Include(p => p.MaNhaCungCapNavigation)
                .Include(p => p.MaNguoiDungNavigation)
                .FirstOrDefaultAsync(p => p.MaPhieuNhap == id);

            if (phieuNhap == null)
            {
                TempData["Error"] = "Không tìm thấy Phiếu Nhập!";
                return RedirectToAction(nameof(Trash));
            }

            // Chốt chặn 1: Kiểm tra Nhà cung cấp
            if (phieuNhap.MaNhaCungCapNavigation.IsDeleted)
            {
                TempData["Error"] = $"Không thể khôi phục phiếu nhập này vì Nhà cung cấp '{phieuNhap.MaNhaCungCapNavigation.TenNhaCungCap}' đang bị xoá. Vui lòng khôi phục Nhà cung cấp trước.";
                return RedirectToAction(nameof(Trash));
            }

            // Chốt chặn 2: Kiểm tra Người lập phiếu (Nhân viên)
            if (phieuNhap.MaNguoiDungNavigation.IsDeleted)
            {
                TempData["Error"] = $"Không thể khôi phục phiếu nhập này vì Nhân viên lập phiếu '{phieuNhap.MaNguoiDungNavigation.HoTen}' đang bị khoá. Vui lòng mở khoá nhân viên trước.";
                return RedirectToAction(nameof(Trash));
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                phieuNhap.IsDeleted = false;
                _context.Update(phieuNhap);

                // Khôi phục các SeriSanPham thuộc phiếu nhập này (những cái bị IsDeleted = true)
                var seriList = await _context.SeriSanPham
                    .Where(s => s.MaPhieuNhap == id && s.IsDeleted)
                    .ToListAsync();

                foreach (var seri in seriList)
                {
                    seri.IsDeleted = false;
                }
                _context.UpdateRange(seriList);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "Đã khôi phục Phiếu Nhập và Seri liên quan thành công.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Lỗi khi khôi phục: " + ex.Message;
            }

            return RedirectToAction(nameof(Trash));
        }

        // 6. KHÔI PHỤC (Đã xử lý ở trên)

        // --- Giữ nguyên các hàm bổ trợ cũ ---
        private bool PhieuNhapExists(int id)
        {
            return _context.PhieuNhap.Any(e => e.MaPhieuNhap == id);
        }

        // 7. TẢI FILE MẪU EXCEL
        [HttpGet]
        public IActionResult DownloadTemplate()
        {
            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("MauNhapKho");

                // Header
                worksheet.Cell(1, 1).Value = "MaSanPham";
                worksheet.Cell(1, 2).Value = "DonGia";
                worksheet.Cell(1, 3).Value = "Seri";

                // Format header
                var headerRange = worksheet.Range("A1:C1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;

                // Dữ liệu mẫu (Vàng)
                worksheet.Cell(2, 1).Value = "CPU001";
                worksheet.Cell(2, 2).Value = 1500000;
                worksheet.Cell(2, 3).Value = "SERI123, SERI456, SERI789";

                worksheet.Columns().AdjustToContents();

                using (var stream = new System.IO.MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Mau_Nhap_Kho.xlsx");
                }
            }
        }
    }
}
