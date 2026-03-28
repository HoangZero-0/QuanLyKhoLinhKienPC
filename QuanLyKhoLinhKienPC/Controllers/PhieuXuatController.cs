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
        public async Task<IActionResult> Index()
        {
            var data = _context.PhieuXuat
                .Where(p => !p.IsDeleted)
                .Include(p => p.MaNguoiDungNavigation)
                .OrderByDescending(p => p.NgayXuat);
            return View(await data.ToListAsync());
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
                .Select(s => new { 
                    MaSanPham = s.MaSanPham, 
                    TenSanPham = s.TenSanPham,
                    GiaBan = s.GiaBan,
                    TonKho = s.SeriSanPham.Count(seri => seri.TrangThai == 1 && !seri.IsDeleted) // Điểm thực tế thẻ seri
                }).ToList();

            ViewBag.SanPhamList = dsSanPham;

            return View();
        }

        // POST: PhieuXuat/Create
        [HttpPost]
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên bán hàng")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TenKhachHang,SoDienThoaiKhach,GhiChu,MaNguoiDung")] PhieuXuat phieuXuat, List<ChiTietBanHang> ChiTietXuat)
        {
            ModelState.Remove("MaNguoiDungNavigation");
            ModelState.Remove("SeriSanPham");
            ModelState.Remove("ChiTietPhieuXuat");

            if (ChiTietXuat == null || ChiTietXuat.Count == 0)
            {
                ModelState.AddModelError("", "Vui lòng thêm ít nhất 1 sản phẩm vào Đơn Bán Hàng!");
            }

            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    phieuXuat.NgayXuat = DateTime.Now;
                    phieuXuat.IsDeleted = false;
                    
                    // Server tự tính tổng tiền từ giỏ hàng thực tế
                    phieuXuat.TongTien = ChiTietXuat.Sum(x => x.SoLuong * x.GiaTien);
                    
                    // 1. LƯU VỎ PHIẾU XUẤT ĐỂ LẤY ID
                    _context.Add(phieuXuat);
                    await _context.SaveChangesAsync();

                    // 2. RÚT SERI KHO TỰ ĐỘNG (XUẤT KHO)
                    var lstChiTietMoi = new List<ChiTietPhieuXuat>();
                    var lstSeriThayDoi = new List<SeriSanPham>();

                    foreach (var mon in ChiTietXuat)
                    {
                        // Quét Database lấy n Seri Đang Tồn Kho của SP này (Take = SoLuong)
                        var nhungSeriRutRa = await _context.SeriSanPham
                            .Where(s => s.MaSanPham == mon.MaSanPham && s.TrangThai == 1 && !s.IsDeleted)
                            .Take(mon.SoLuong)
                            .ToListAsync();

                        if (nhungSeriRutRa.Count < mon.SoLuong)
                        {
                            throw new Exception($"Không đủ Số lượng Seri Tồn tại Kho cho mã sản phẩm [{mon.MaSanPham}] !");
                        }

                        // Lặp qua những seri cầm trên tay, lột xác chúng
                        foreach (var sr in nhungSeriRutRa)
                        {
                            // Đánh dấu là đã Bán & Thuộc về Phiếu xuất nào (Tracking)
                            sr.TrangThai = 2;
                            sr.MaPhieuXuat = phieuXuat.MaPhieuXuat;
                            lstSeriThayDoi.Add(sr);

                            // Tạo 1 dòng chứng từ Hóa Đơn Detail kẹp chung với Seri đó
                            var ctt = new ChiTietPhieuXuat
                            {
                                MaSeri = sr.MaSeri,
                                MaPhieuXuat = phieuXuat.MaPhieuXuat,
                                GiaTien = mon.GiaTien
                            };
                            lstChiTietMoi.Add(ctt);
                        }
                    }

                    // 3. ĐỔ TOÀN BỘ LIST SỰ THAY ĐỔI VÀO DB
                    _context.SeriSanPham.UpdateRange(lstSeriThayDoi);
                    _context.ChiTietPhieuXuat.AddRange(lstChiTietMoi);
                    await _context.SaveChangesAsync();

                    // 4. CHỐT ĐƠN (COMMIT)
                    await transaction.CommitAsync();

                    TempData["Success"] = "Lập Hóa Đơn và Rút Kho Seri Bán Hàng Thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Lỗi Rút Kho Thực Tế: " + ex.Message + " Toàn bộ giao dịch ảo đã được Rollback. Không có dữ liệu nào bị thay đổi.");
                }
            }

            ViewData["MaNguoiDung"] = new SelectList(_context.NguoiDung, "MaNguoiDung", "HoTen", phieuXuat.MaNguoiDung);
            
            var dsSanPham = _context.SanPham.Where(s => !s.IsDeleted).Select(s => new { MaSanPham = s.MaSanPham, TenSanPham = s.TenSanPham, GiaBan = s.GiaBan, TonKho = s.SeriSanPham.Count(seri => seri.TrangThai == 1 && !seri.IsDeleted) }).ToList();
            ViewBag.SanPhamList = dsSanPham;

            return View(phieuXuat);
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

            var phieuXuat = await _context.PhieuXuat.FindAsync(id);
            if (phieuXuat == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaNguoiDung"] = new SelectList(_context.NguoiDung, "MaNguoiDung", "MatKhau", phieuXuat.MaNguoiDung);
            return View(phieuXuat);
        }

        // POST: PhieuXuat/Edit/5
        [HttpPost]
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên bán hàng")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaPhieuXuat,NgayXuat,TenKhachHang,SoDienThoaiKhach,TongTien,MaNguoiDung,IsDeleted")] PhieuXuat phieuXuat)
        {
            if (id != phieuXuat.MaPhieuXuat)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(phieuXuat);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PhieuXuatExists(phieuXuat.MaPhieuXuat))
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
            ViewData["MaNguoiDung"] = new SelectList(_context.NguoiDung, "MaNguoiDung", "MatKhau", phieuXuat.MaNguoiDung);
            return View(phieuXuat);
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
                    await transaction.CommitAsync();

                    TempData["Success"] = "Đã chuyển Hóa đơn xuất vào thùng rác và hoàn trả Seri về kho hàng.";
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = "Lỗi khi xóa Hóa đơn: " + ex.Message;
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // 6. THÙNG RÁC (Hiện danh sách đã xóa)
        // GET: PhieuXuat/Trash
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên bán hàng")]
        public async Task<IActionResult> Trash()
        {
            var data = _context.PhieuXuat
                .Where(p => p.IsDeleted)
                .Include(p => p.MaNguoiDungNavigation)
                .OrderByDescending(p => p.NgayXuat);
            return View(await data.ToListAsync());
        }

        // 7. KHÔI PHỤC (Hồi sinh từ thùng rác)
        // POST: PhieuXuat/Restore
        [HttpPost]
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên bán hàng")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var phieuXuat = await _context.PhieuXuat.FindAsync(id);
            if (phieuXuat == null)
            {
                TempData["Error"] = "Không tìm thấy Phiếu Xuất!";
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
                await transaction.CommitAsync();

                TempData["Success"] = "Đã khôi phục Hóa Đơn và rút lại Seri thành công.";
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
    }
}
