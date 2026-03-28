USE QuanLyKhoLinhKienPC;
GO

-- 1. Xóa dữ liệu theo thứ tự từ Bảng Con -> Bảng Cha
DELETE FROM ChiTietPhieuXuat;
DELETE FROM ChiTietPhieuNhap;

DELETE FROM SeriSanPham;

DELETE FROM PhieuXuat;
DELETE FROM PhieuNhap;

DELETE FROM SanPham;
DELETE FROM DanhMuc;

DELETE FROM NhaCungCap;
DELETE FROM NguoiDung;
DELETE FROM VaiTro;
GO

-- 2. Reset lại cột tự tăng (IDENTITY) về 0 cho tất cả các bảng
-- Bản ghi INSERT tiếp theo sẽ có ID = 1
DBCC CHECKIDENT ('ChiTietPhieuXuat', RESEED, 0);
DBCC CHECKIDENT ('ChiTietPhieuNhap', RESEED, 0);

DBCC CHECKIDENT ('SeriSanPham', RESEED, 0);

DBCC CHECKIDENT ('PhieuXuat', RESEED, 0);
DBCC CHECKIDENT ('PhieuNhap', RESEED, 0);

DBCC CHECKIDENT ('SanPham', RESEED, 0);
DBCC CHECKIDENT ('DanhMuc', RESEED, 0);

DBCC CHECKIDENT ('NhaCungCap', RESEED, 0);
DBCC CHECKIDENT ('NguoiDung', RESEED, 0);
DBCC CHECKIDENT ('VaiTro', RESEED, 0);
GO

-- =======================================================
-- 1. CHÈN DỮ LIỆU DANH MỤC TRƯỚC (Vì nó làm Khóa Ngoại)
-- =======================================================
INSERT INTO VaiTro (TenVaiTro, IsDeleted) VALUES 
(N'Quản trị viên', 0), 
(N'Nhân viên kho', 0), 
(N'Nhân viên bán hàng', 0);
GO
INSERT INTO DanhMuc (TenDanhMuc, IsDeleted) VALUES 
(N'Vi xử lý (CPU)', 0),
(N'Bo mạch chủ (Mainboard)', 0),
(N'Bộ nhớ trong (RAM)', 0),
(N'Ổ cứng (SSD/HDD)', 0),
(N'Card màn hình (VGA)', 0),
(N'Nguồn máy tính (PSU)', 0),
(N'Vỏ máy tính (Case)', 0);
GO
INSERT INTO NhaCungCap (TenNhaCungCap, SoDienThoai, DiaChi, IsDeleted) VALUES 
(N'Công Ty GearVN', '02871081881', N'Hoàng Hoa Thám, Tân Bình, TP.HCM', 0),
(N'Nhà Phân Phối Viễn Sơn', '02838326085', N'Nguyễn Đình Chiểu, Quận 3, TP.HCM', 0),
(N'FPT Trading', '02473006666', N'Duy Tân, Cầu Giấy, Hà Nội', 0);
GO
-- =======================================================
-- 2. CHÈN DỮ LIỆU NGƯỜI DÙNG
-- Lưu ý: Mật khẩu '123456' đã được băm (Hash) bằng BCrypt
-- Chuỗi '$2a$11$0z...' là chuỗi mã hóa an toàn của 123456
-- =======================================================
INSERT INTO NguoiDung (TenDangNhap, MatKhau, HoTen, Email, MaVaiTro, IsDeleted) VALUES 
('admin', '$2a$11$ybZg6y9iHXbqDpJhEQ70z.aO16lgAy7V6ZNYC0EKmUedH6gNqTghy', N'Admin Nguyễn Thức', 'admin@gmail.com', 1, 0),
('kho', '$2a$11$ybZg6y9iHXbqDpJhEQ70z.aO16lgAy7V6ZNYC0EKmUedH6gNqTghy', N'Kho Trần Minh', 'kho@gmail.com', 2, 0),
('banhang', '$2a$11$ybZg6y9iHXbqDpJhEQ70z.aO16lgAy7V6ZNYC0EKmUedH6gNqTghy', N'Sale Lê Thảo', 'sale@gmail.com', 3, 0);
GO
-- =======================================================
-- 3. CHÈN SẢN PHẨM VẬT TƯ
-- =======================================================
INSERT INTO SanPham (TenSanPham, HangSanXuat, HinhAnh, GiaBan, MaDanhMuc, ThongSoKyThuat, ThoiGianBaoHanh, IsDeleted) VALUES 
-- SP 1
(N'Intel Core i5 12400F / 2.5GHz / 6 Nhân 12 Luồng', 'Intel',
 N'https://product.hstatic.net/200000722513/product/i5-12400f.png', 3500000, 1, 
 N'Socket: LGA1700' + NCHAR(10) + N'Cores: 6' + NCHAR(10) + N'Threads: 12', 36, 0),
-- SP 2
(N'RAM Kingston Fury Beast 8GB DDR4 3200MHz', 'Kingston',
 N'https://bizweb.dktcdn.net/thumb/1024x1024/100/436/596/products/kingston.jpg', 650000, 3, 
 N'Dung lượng: 8GB' + NCHAR(10) + N'Bus: 3200MHz', 36, 0),
-- SP 3
(N'VGA ASUS TUF Gaming GeForce RTX 3060 12GB', 'ASUS',
 N'https://product.hstatic.net/1000026716/product/tuf-rtx3060.png', 8900000, 5, 
 N'VRAM: 12GB' + NCHAR(10) + N'Nguồn: 650W', 36, 0);
GO
-- =======================================================
-- 4. LUỒNG NGHIỆP VỤ NHẬP KHO (Mua 5 thanh RAM)
-- =======================================================
-- Bước A: Tạo Phiếu Nhập
INSERT INTO PhieuNhap (NgayNhap, TongTien, GhiChu, MaNhaCungCap, MaNguoiDung, IsDeleted) VALUES 
('2026-03-01 08:30:00', 3000000, N'Nhập nguyên lô RAM đầu tháng test máy', 1, 1, 0);
GO
-- Bước B: Gắn Chi tiết (5 thanh RAM, giá nhập 600K/thanh => Tổng 3 Triệu)
INSERT INTO ChiTietPhieuNhap (SoLuong, DonGiaNhap, MaPhieuNhap, MaSanPham) VALUES 
(5, 600000, 1, 2);
GO
-- Bước C: Nảy ra 5 thẻ Seri cho 5 thanh RAM đó để quản lý chặc chẽ trong kho
-- TrangThai = 1 (Tức là đang Tồn Kho)
INSERT INTO SeriSanPham (SoSeri, TrangThai, MaSanPham, MaPhieuNhap, MaPhieuXuat, IsDeleted) VALUES 
('PN1-SP2-0301-A1BC', 1, 2, 1, NULL, 0),
('PN1-SP2-0301-X9TZ', 1, 2, 1, NULL, 0),
('PN1-SP2-0301-KL2M', 1, 2, 1, NULL, 0),
('PN1-SP2-0301-QW9E', 1, 2, 1, NULL, 0),
('PN1-SP2-0301-P0O9', 1, 2, 1, NULL, 0);
GO
-- =======================================================
-- 5. LUỒNG NGHIỆP VỤ XUẤT KHO / BÁN HÀNG
-- (Khách đến mua 2 thanh RAM vừa nhập ở trên)
-- =======================================================
-- Bước A: Tạo Hóa Đơn Xuất
INSERT INTO PhieuXuat (NgayXuat, TenKhachHang, SoDienThoaiKhach, TongTien, MaNguoiDung, IsDeleted) VALUES 
('2026-03-14 10:15:00', N'Nguyễn Khang', '0901239999', 1300000, 3, 0);
GO
-- Bước B: Gắn Chi Tiết Bán Cụ thể Từng Seri
-- Giả sử hệ thống Random bán đi cái Seri đuôi `A1BC` (Mã ID 1) và `QW9E` (Mã ID 4)
INSERT INTO ChiTietPhieuXuat (GiaTien, MaPhieuXuat, MaSeri) VALUES 
(650000, 1, 1), -- Bán Seri số 1 (A1BC)
(650000, 1, 4); -- Bán Seri số 4 (QW9E)
GO
-- Bước C: Đổi trạng thái Kho của 2 Seri đó thành ĐÃ BÁN
-- TrangThai = 2 (Đã bay khỏi kho)
UPDATE SeriSanPham 
SET TrangThai = 2, MaPhieuXuat = 1 
WHERE MaSeri IN (1, 4);
GO