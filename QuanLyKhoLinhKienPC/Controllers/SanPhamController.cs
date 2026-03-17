using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyKhoLinhKienPC.Models;
using Microsoft.AspNetCore.Authorization;

namespace QuanLyKhoLinhKienPC.Controllers
{
    [Authorize]
    public class SanPhamController : Controller
    {
        private readonly QuanLyKhoLinhKienPCContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public SanPhamController(QuanLyKhoLinhKienPCContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // 1. DANH SÁCH (Chỉ hiện cái chưa xóa)
        // GET: SanPham
        public async Task<IActionResult> Index(string searchString, int? MaDanhMuc, string? HangSanXuat)
        {
            var dsSanPham = _context.SanPham
                .Include(s => s.MaDanhMucNavigation)
                .Include(s => s.SeriSanPham)
                .Where(d => d.IsDeleted == false);

            if (!string.IsNullOrEmpty(searchString))
            {
                dsSanPham = dsSanPham.Where(d => d.TenSanPham.Contains(searchString) || d.HangSanXuat.Contains(searchString));
            }

            if (MaDanhMuc.HasValue)
            {
                dsSanPham = dsSanPham.Where(d => d.MaDanhMuc == MaDanhMuc);
            }

            if (!string.IsNullOrEmpty(HangSanXuat))
            {
                dsSanPham = dsSanPham.Where(d => d.HangSanXuat == HangSanXuat);
            }

            var list = await dsSanPham.ToListAsync();

            // Tính tồn kho ảo
            foreach (var item in list)
            {
                item.SoLuongTon = item.SeriSanPham.Count();
            }

            // Lấy danh sách Hãng sản xuất duy nhất
            var hangSanXuatList = await _context.SanPham
                .Where(s => !s.IsDeleted && !string.IsNullOrEmpty(s.HangSanXuat))
                .Select(s => s.HangSanXuat)
                .Distinct()
                .ToListAsync();

            ViewData["MaDanhMuc"] = new SelectList(_context.DanhMuc.Where(d => !d.IsDeleted), "MaDanhMuc", "TenDanhMuc", MaDanhMuc);
            ViewBag.HangSanXuatList = new SelectList(hangSanXuatList, HangSanXuat);
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentHangSanXuat"] = HangSanXuat;

            return View(list);
        }

        // 2. CHI TIẾT
        // GET: SanPham/Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            var sanPham = await _context.SanPham
                .Include(s => s.MaDanhMucNavigation)
                .Include(s => s.SeriSanPham)
                .FirstOrDefaultAsync(m => m.MaSanPham == id);

            if (sanPham == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            sanPham.SoLuongTon = sanPham.SeriSanPham.Count();

            return View(sanPham);
        }

        // 3. TẠO MỚI
        // GET: SanPham/Create
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
        public IActionResult Create()
        {
            ViewData["MaDanhMuc"] = new SelectList(_context.DanhMuc.Where(d => !d.IsDeleted), "MaDanhMuc", "TenDanhMuc");
            return View();
        }

        // POST: SanPham/Create
        [HttpPost]
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaSanPham,TenSanPham,HangSanXuat,GiaBan,ThongSoKyThuat,ThoiGianBaoHanh,MaDanhMuc,IsDeleted")] SanPham sanPham, IFormFile? fHinhAnh)
        {
            // Bỏ qua Validate mặc định của EF Core cho các cột không bắt buộc hoặc tự sinh
            ModelState.Remove("HinhAnh");
            ModelState.Remove("fHinhAnh");
            ModelState.Remove("ThongSoKyThuat");
            ModelState.Remove("MaDanhMucNavigation");
            ModelState.Remove("ChiTietPhieuNhap");
            ModelState.Remove("SeriSanPham");

            // Kiểm tra File Hình Ảnh (Tùy chọn)
            if (fHinhAnh != null && fHinhAnh.Length > 0)
            {
                if (!IsValidImage(fHinhAnh, out string error))
                {
                    ModelState.AddModelError("HinhAnh", error);
                }
                else
                {
                    string fileName = Guid.NewGuid().ToString() + ".webp";
                    string uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "sanpham");

                    if (!Directory.Exists(uploadPath))
                    {
                        Directory.CreateDirectory(uploadPath);
                    }

                    string filePath = Path.Combine(uploadPath, fileName);

                    // Khắc phục triệt để lỗi viền đen bằng cách tự dựng Canvas chuẩn 800x800 lót nền Trắng
                    using (var sourceImage = await Image.LoadAsync(fHinhAnh.OpenReadStream()))
                    {
                        // 1. Thu nhỏ ảnh gốc sao cho vừa lọt khung 800 (giữ nguyên gốc rễ, không độn thêm)
                        sourceImage.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new Size(800, 800),
                            Mode = ResizeMode.Max
                        }));

                        // 2. Tạo tờ giấy trắng tinh 800x800
                        using (var destImage = new Image<Rgba32>(800, 800))
                        {
                            destImage.Mutate(x => x.BackgroundColor(SixLabors.ImageSharp.Color.White));

                            // 3. Tính tọa độ dán ảnh vào chính giữa tờ giấy trắng
                            int x = (800 - sourceImage.Width) / 2;
                            int y = (800 - sourceImage.Height) / 2;

                            // 4. Dán chồng ảnh lên và xuất ra WebP
                            destImage.Mutate(c => c.DrawImage(sourceImage, new Point(x, y), 1f));
                            
                            await destImage.SaveAsWebpAsync(filePath, new SixLabors.ImageSharp.Formats.Webp.WebpEncoder
                            {
                                Quality = 75
                            });
                        }
                    }

                    // Lưu đường dẫn DB
                    sanPham.HinhAnh = "/images/sanpham/" + fileName;
                }
            }
            else
            {
                sanPham.HinhAnh = "";
            }

            if (ModelState.IsValid)
            {
                _context.Add(sanPham);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm mới sản phẩm thành công!";
                return RedirectToAction(nameof(Index));
            }

            ViewData["Error"] = "Vui lòng kiểm tra lại thông tin nhập!";
            ViewData["MaDanhMuc"] = new SelectList(_context.DanhMuc.Where(d => !d.IsDeleted), "MaDanhMuc", "TenDanhMuc", sanPham.MaDanhMuc);
            return View(sanPham);
        }

        // 4. CHỈNH SỬA
        // GET: SanPham/Edit
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            var sanPham = await _context.SanPham.FindAsync(id);
            if (sanPham == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            ViewData["MaDanhMuc"] = new SelectList(_context.DanhMuc.Where(d => !d.IsDeleted), "MaDanhMuc", "TenDanhMuc", sanPham.MaDanhMuc);
            return View(sanPham);
        }

        // POST: SanPham/Edit
        [HttpPost]
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaSanPham,TenSanPham,HangSanXuat,HinhAnh,GiaBan,ThongSoKyThuat,ThoiGianBaoHanh,MaDanhMuc,IsDeleted")] SanPham sanPham, IFormFile? fHinhAnh)
        {
            if (id != sanPham.MaSanPham)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            // Bỏ qua Validate mặc định của EF Core cho các cột không bắt buộc hoặc tự sinh
            ModelState.Remove("HinhAnh");
            ModelState.Remove("fHinhAnh");
            ModelState.Remove("ThongSoKyThuat");
            ModelState.Remove("MaDanhMucNavigation");
            ModelState.Remove("ChiTietPhieuNhap");
            ModelState.Remove("SeriSanPham");

            // Lưu ý: Ở hàm Edit, fHinhAnh CÓ THỂ null (nếu người dùng không muốn đổi ảnh mới). 
            // Do đó không bắt lỗi ModelState.AddModelError cho fHinhAnh ở đây.

            // KIỂM TRA ẢNH MỚI (Nếu có tải lên)
            if (fHinhAnh != null && fHinhAnh.Length > 0)
            {
                if (!IsValidImage(fHinhAnh, out string error))
                {
                    ModelState.AddModelError("HinhAnh", error);
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (fHinhAnh != null)
                    {
                        // 1. Xóa ảnh cũ (nếu có và không phải ảnh mặc định)
                        DeleteOldImage(sanPham.HinhAnh);

                        // 2. Upload ảnh mới
                        string folderName = "images/sanpham";
                        string webRootPath = _webHostEnvironment.WebRootPath;
                        string path = Path.Combine(webRootPath, folderName);
                        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                        string fileName = Guid.NewGuid().ToString() + ".webp";
                        string fullPath = Path.Combine(path, fileName);

                        using (var sourceImage = await Image.LoadAsync(fHinhAnh.OpenReadStream()))
                        {
                            sourceImage.Mutate(x => x.Resize(new ResizeOptions
                            {
                                Size = new Size(800, 800),
                                Mode = ResizeMode.Max
                            }));

                            using (var destImage = new Image<Rgba32>(800, 800))
                            {
                                destImage.Mutate(x => x.BackgroundColor(SixLabors.ImageSharp.Color.White));

                                int x = (800 - sourceImage.Width) / 2;
                                int y = (800 - sourceImage.Height) / 2;

                                destImage.Mutate(c => c.DrawImage(sourceImage, new Point(x, y), 1f));
                                
                                await destImage.SaveAsWebpAsync(fullPath, new SixLabors.ImageSharp.Formats.Webp.WebpEncoder
                                {
                                    Quality = 75
                                });
                            }
                        }

                        sanPham.HinhAnh = "/" + folderName + "/" + fileName;
                    }

                    _context.Update(sanPham);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật sản phẩm thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SanPhamExists(sanPham.MaSanPham))
                    {
                        TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            // PHỤC HỒI ẢNH CŨ CHO VIEW NẾU VALIDATE LỖI (Tránh bị mất Ảnh trên View khi nhấn Lưu)
            if (string.IsNullOrEmpty(sanPham.HinhAnh))
            {
                var oldSp = await _context.SanPham.AsNoTracking().FirstOrDefaultAsync(s => s.MaSanPham == id);
                if (oldSp != null)
                {
                    sanPham.HinhAnh = oldSp.HinhAnh;
                }
            }

            ViewData["Error"] = "Vui lòng kiểm tra lại thông tin nhập!";
            ViewData["MaDanhMuc"] = new SelectList(_context.DanhMuc.Where(d => !d.IsDeleted), "MaDanhMuc", "TenDanhMuc", sanPham.MaDanhMuc);
            return View(sanPham);
        }

        // 5. XÓA MỀM (Chuyển vào thùng rác)
        // GET: SanPham/Delete
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            var sanPham = await _context.SanPham
                .Include(s => s.MaDanhMucNavigation)
                .FirstOrDefaultAsync(m => m.MaSanPham == id);

            if (sanPham == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Index));
            }

            return View(sanPham);
        }

        // POST: SanPham/Delete
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var sanPham = await _context.SanPham.FindAsync(id);
            if (sanPham != null)
            {
                // Logic xóa mềm
                sanPham.IsDeleted = true;
                _context.Update(sanPham);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã chuyển sản phẩm vào thùng rác.";
            }
            return RedirectToAction(nameof(Index));
        }

        // 6. THÙNG RÁC (Hiện danh sách đã xóa)
        // GET: SanPham/Trash
        public async Task<IActionResult> Trash(string searchString, int? MaDanhMuc, string? HangSanXuat)
        {
            var dsSanPham = _context.SanPham
                .Include(s => s.MaDanhMucNavigation)
                .Where(d => d.IsDeleted == true);

            if (!string.IsNullOrEmpty(searchString))
            {
                dsSanPham = dsSanPham.Where(d => d.TenSanPham.Contains(searchString) || d.HangSanXuat.Contains(searchString));
            }

            if (MaDanhMuc.HasValue)
            {
                dsSanPham = dsSanPham.Where(d => d.MaDanhMuc == MaDanhMuc);
            }

            if (!string.IsNullOrEmpty(HangSanXuat))
            {
                dsSanPham = dsSanPham.Where(d => d.HangSanXuat == HangSanXuat);
            }

            // Lấy danh sách Hãng sản xuất duy nhất từ thùng rác hoặc tất cả
            var hangSanXuatList = await _context.SanPham
                .Where(s => !string.IsNullOrEmpty(s.HangSanXuat))
                .Select(s => s.HangSanXuat)
                .Distinct()
                .ToListAsync();

            ViewData["MaDanhMuc"] = new SelectList(_context.DanhMuc.Where(d => !d.IsDeleted), "MaDanhMuc", "TenDanhMuc", MaDanhMuc);
            ViewBag.HangSanXuatList = new SelectList(hangSanXuatList, HangSanXuat);
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentHangSanXuat"] = HangSanXuat;

            return View(await dsSanPham.ToListAsync());
        }

        // 7. KHÔI PHỤC (Hồi sinh từ thùng rác)
        // POST: SanPham/Restore
        [HttpPost]
        [Authorize(Roles = "Quản trị viên,Admin,Nhân viên kho")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var sanPham = await _context.SanPham.FindAsync(id);
            if (sanPham == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Trash));
            }

            sanPham.IsDeleted = false;
            _context.Update(sanPham);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Khôi phục sản phẩm thành công.";
            return RedirectToAction(nameof(Trash));
        }

        // 8. XÓA VĨNH VIỄN (Chỉ xóa được khi không có ràng buộc)
        // POST: SanPham/DeleteForce
        [HttpPost]
        [Authorize(Roles = "Quản trị viên,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteForce(int id)
        {
            var sanPham = await _context.SanPham.FindAsync(id);
            if (sanPham == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu yêu cầu!";
                return RedirectToAction(nameof(Trash));
            }

            try
            {
                // Lưu đường dẫn ảnh trước khi xóa DB
                string imagePath = sanPham.HinhAnh;

                _context.SanPham.Remove(sanPham);
                await _context.SaveChangesAsync();

                // Nếu xóa DB thành công thì mới xóa ảnh
                DeleteOldImage(imagePath);

                TempData["Success"] = "Đã xóa vĩnh viễn sản phẩm và hình ảnh.";
                return RedirectToAction(nameof(Trash));
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Không thể xóa vĩnh viễn sản phẩm này vì đã có dữ liệu nhập/xuất kho liên quan!";
                return RedirectToAction(nameof(Trash));
            }
        }

        // 9. DỌN SẠCH THÙNG RÁC (Phiên bản thông minh: Xóa được bao nhiêu thì xóa)
        [HttpPost]
        [Authorize(Roles = "Quản trị viên,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmptyTrash()
        {
            // Lấy tất cả danh sách trong thùng rác
            var racList = await _context.SanPham.Where(v => v.IsDeleted == true).ToListAsync();

            if (!racList.Any())
            {
                return RedirectToAction(nameof(Trash));
            }

            int daXoa = 0;
            int biLoi = 0;

            foreach (var item in racList)
            {
                try
                {
                    string imagePath = item.HinhAnh; // Lưu lại đường dẫn ảnh

                    // Cố gắng xóa từng cái
                    _context.SanPham.Remove(item);
                    await _context.SaveChangesAsync(); // Xóa trong DB

                    DeleteOldImage(imagePath); // Xóa file ảnh

                    daXoa++;
                }
                catch (DbUpdateException)
                {
                    // Nếu lỗi (do ràng buộc khóa ngoại), bỏ qua và đếm lỗi
                    biLoi++;

                    // QUAN TRỌNG: Phải reset trạng thái của item bị lỗi về "Chưa thay đổi"
                    // Nếu không, EF Core sẽ vẫn nhớ lệnh xóa này và gây lỗi cho item tiếp theo
                    _context.Entry(item).State = EntityState.Unchanged;
                }
            }

            // Thông báo kết quả cho người dùng
            if (daXoa > 0 && biLoi == 0)
            {
                TempData["Success"] = $"Đã dọn sạch thùng rác ({daXoa} sản phẩm).";
            }
            else if (daXoa > 0 && biLoi > 0)
            {
                TempData["Warning"] = $"Đã xóa vĩnh viễn {daXoa} sản phẩm. Còn lại {biLoi} sản phẩm không thể xóa do đang được sử dụng.";
            }
            else if (daXoa == 0 && biLoi > 0)
            {
                TempData["Error"] = "Không thể xóa sản phẩm nào vì tất cả đều đang được sử dụng!";
            }

            return RedirectToAction(nameof(Trash));
        }

        // 10. HÀM RIÊNG ĐỂ XÓA ẢNH KHỎI Ổ CỨNG
        private void DeleteOldImage(string imagePath)
        {
            if (!string.IsNullOrEmpty(imagePath) && !imagePath.Contains("no-image.png"))
            {
                try
                {
                    // Chuyển đổi đường dẫn web (/images/...) thành đường dẫn ổ cứng (C:\...)
                    string webRootPath = _webHostEnvironment.WebRootPath;
                    string relativePath = imagePath.TrimStart('/'); // Bỏ dấu / đầu tiên
                    string absolutePath = Path.Combine(webRootPath, relativePath);

                    if (System.IO.File.Exists(absolutePath))
                    {
                        System.IO.File.Delete(absolutePath);
                    }
                }
                catch
                {
                    // Nếu lỗi xóa file thì bỏ qua, không làm crash ứng dụng
                }
            }
        }

        // 11. HÀM KIỂM TRA FILE ẢNH KHI TẢI LÊN
        private bool IsValidImage(IFormFile file, out string errorMessage)
        {
            // 1. Kiểm tra đuôi file
            var supportedTypes = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var fileExt = Path.GetExtension(file.FileName).ToLower();
            if (!supportedTypes.Contains(fileExt))
            {
                errorMessage = "Chỉ chấp nhận ảnh định dạng .jpg, .jpeg, .png, .webp!";
                return false;
            }

            // 2. Kiểm tra dung lượng (Ví dụ: 2MB = 2 * 1024 * 1024 bytes)
            if (file.Length > 2 * 1024 * 1024)
            {
                errorMessage = "Dung lượng ảnh không được vượt quá 2MB!";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        private bool SanPhamExists(int id)
        {
            return _context.SanPham.Any(e => e.MaSanPham == id);
        }
    }
}