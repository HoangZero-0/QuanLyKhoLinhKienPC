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
    public class PhieuNhapController : Controller
    {
        private readonly QuanLyKhoLinhKienPCContext _context;

        public PhieuNhapController(QuanLyKhoLinhKienPCContext context)
        {
            _context = context;
        }

        // 1. DANH SÁCH
        // GET: PhieuNhap
        public async Task<IActionResult> Index()
        {
            var data = _context.PhieuNhap
                .Where(p => !p.IsDeleted)
                .Include(p => p.MaNguoiDungNavigation)
                .Include(p => p.MaNhaCungCapNavigation)
                .OrderByDescending(p => p.NgayNhap);
            return View(await data.ToListAsync());
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
        public async Task<IActionResult> Create([Bind("GhiChu,MaNhaCungCap,MaNguoiDung,ChiTietPhieuNhap")] PhieuNhap phieuNhap)
        {
            // Bỏ qua Validate cho các thuộc tính tự tính và Khóa ngoại Navigation
            ModelState.Remove("MaNguoiDungNavigation");
            ModelState.Remove("MaNhaCungCapNavigation");
            ModelState.Remove("SeriSanPham");

            if (phieuNhap.ChiTietPhieuNhap == null || phieuNhap.ChiTietPhieuNhap.Count == 0)
            {
                ModelState.AddModelError("", "Vui lòng thêm ít nhất 1 Sản Phẩm vào Phiếu Nhập!");
            }

            if (ModelState.IsValid)
            {
                // Bổ sung các trường tự động
                phieuNhap.NgayNhap = DateTime.Now;
                phieuNhap.IsDeleted = false;

                // Server tự tính lại Tổng Tiền phòng Front-end bị thay đổi
                phieuNhap.TongTien = phieuNhap.ChiTietPhieuNhap.Sum(ct => ct.SoLuong * ct.DonGiaNhap);

                // DÙNG TRANSACTION ĐỂ BẢO VỆ DỮ LIỆU
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // 1. Lưu Phiếu Nhập và Chi Tiết cùng 1 lúc
                        _context.Add(phieuNhap);
                        await _context.SaveChangesAsync();

                        // 2. SINH SERI SẢN PHẨM TỰ ĐỘNG
                        var random = new Random();
                        string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                        var lstSeriMoi = new List<SeriSanPham>();

                        foreach (var chitiet in phieuNhap.ChiTietPhieuNhap)
                        {
                            for (int i = 0; i < chitiet.SoLuong; i++)
                            {
                                // Tạo chuỗi ngẫu nhiên 3 ký tự gắn mác tránh trùng lặp
                                string randomEnd = new string(Enumerable.Repeat(chars, 4).Select(s => s[random.Next(s.Length)]).ToArray());

                                // Format: [Mã PhieuNhap]-[Mã SP]-[NgayNhap]-[ChuoiRandom] (Ví dụ: PN7-SP2-26032026-X8Y1)
                                string soSeriPhatSinh = $"PN{phieuNhap.MaPhieuNhap}-SP{chitiet.MaSanPham}-{DateTime.Now:ddMM}-{randomEnd}";

                                var seriMoi = new SeriSanPham
                                {
                                    SoSeri = soSeriPhatSinh,
                                    TrangThai = 1, // 1 = Chưa Bán (Trong Kho)
                                    MaSanPham = chitiet.MaSanPham,
                                    MaPhieuNhap = phieuNhap.MaPhieuNhap,
                                    IsDeleted = false
                                };
                                lstSeriMoi.Add(seriMoi);
                            }
                        }

                        // 3. Đổ kho Seri vào Lưu hàng loạt (Tốc độ cao)
                        _context.SeriSanPham.AddRange(lstSeriMoi);
                        await _context.SaveChangesAsync();

                        // Hoàn tất 3 Cấp độ lưu trữ (Commit)
                        await transaction.CommitAsync();

                        TempData["Success"] = "Tạo Phiếu Nhập và sinh mã Seri thành công!";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception)
                    {
                        // Nếu lỡ rớt mạng hoặc đứt đoạn, Hủy toàn bộ Phiếu Nhập, Chi Tiết và Seri vừa tạo
                        await transaction.RollbackAsync();
                        ModelState.AddModelError("", "Đã có lỗi xảy ra trong quá trình sinh mã Seri. Hệ thống đã hủy tác vụ để bảo vệ dữ liệu nền.");
                    }
                }
            }

            TempData["Error"] = "Vui lòng xem lại thông tin!";
            ViewData["MaNguoiDung"] = new SelectList(_context.NguoiDung, "MaNguoiDung", "HoTen", phieuNhap.MaNguoiDung);
            ViewData["MaNhaCungCap"] = new SelectList(_context.NhaCungCap.Where(n => !n.IsDeleted), "MaNhaCungCap", "TenNhaCungCap", phieuNhap.MaNhaCungCap);
            ViewBag.SanPhamList = _context.SanPham.Where(s => !s.IsDeleted).Select(s => new { MaSanPham = s.MaSanPham, TenSanPham = s.TenSanPham }).ToList();

            return View(phieuNhap);
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

            var phieuNhap = await _context.PhieuNhap.FindAsync(id);
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
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 1. Soft delete phiếu nhập
                    phieuNhap.IsDeleted = true;
                    _context.Update(phieuNhap);

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
        public async Task<IActionResult> Trash()
        {
            var data = _context.PhieuNhap
                .Where(p => p.IsDeleted)
                .Include(p => p.MaNguoiDungNavigation)
                .Include(p => p.MaNhaCungCapNavigation)
                .OrderByDescending(p => p.NgayNhap);
            return View(await data.ToListAsync());
        }

        // 7. KHÔI PHỤC (Hồi sinh từ thùng rác)
        // POST: PhieuNhap/Restore
        [HttpPost]
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var phieuNhap = await _context.PhieuNhap.FindAsync(id);
            if (phieuNhap == null)
            {
                TempData["Error"] = "Không tìm thấy Phiếu Nhập!";
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

        private bool PhieuNhapExists(int id)
        {
            return _context.PhieuNhap.Any(e => e.MaPhieuNhap == id);
        }
    }
}
