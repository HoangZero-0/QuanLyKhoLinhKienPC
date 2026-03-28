-- I. Tạo Database
CREATE DATABASE QuanLyKhoLinhKienPC;
GO

USE QuanLyKhoLinhKienPC;
GO

-- 1. Bảng VaiTro (Role): Quản trị viên, Nhân viên kho, Nhân viên bán hàng...

CREATE TABLE VaiTro (
    MaVaiTro INT IDENTITY(1,1) PRIMARY KEY,
    TenVaiTro NVARCHAR(50) NOT NULL,
    IsDeleted BIT NOT NULL DEFAULT 0   -- 0: Hoạt động, 1: Đã xóa
);
GO

-- 2. Bảng NguoiDung (User): Tài khoản đăng nhập

CREATE TABLE NguoiDung (
    MaNguoiDung INT IDENTITY(1,1) PRIMARY KEY,   
    TenDangNhap VARCHAR(50) NOT NULL UNIQUE,   -- Không trùng tên đăng nhập
    MatKhau VARCHAR(255) NOT NULL,   -- Lưu Hash, không lưu text thường
    HoTen NVARCHAR(100) NULL,
    Email VARCHAR(100) NULL,
    MaVaiTro INT NOT NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_NguoiDung_VaiTro FOREIGN KEY (MaVaiTro) REFERENCES VaiTro(MaVaiTro)
);
GO

-- 3. Bảng NhaCungCap (Supplier): Đối tác nhập hàng

CREATE TABLE NhaCungCap (
    MaNhaCungCap INT IDENTITY(1,1) PRIMARY KEY,
    TenNhaCungCap NVARCHAR(200) NOT NULL,
    SoDienThoai VARCHAR(20) NULL,
    DiaChi NVARCHAR(MAX) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0
);
GO

-- 4. Bảng DanhMuc (Category): CPU, RAM, VGA...

CREATE TABLE DanhMuc (
    MaDanhMuc INT IDENTITY(1,1) PRIMARY KEY,
    TenDanhMuc NVARCHAR(100) NOT NULL,
    IsDeleted BIT NOT NULL DEFAULT 0
);
GO

-- 5. Bảng SanPham (Product): Mẫu mã chung (Model)
-- LƯU Ý: Không có cột SoLuongTon ở đây (Đã tuân thủ quy tắc cột ảo NotMapped)

CREATE TABLE SanPham (
    MaSanPham INT IDENTITY(1,1) PRIMARY KEY,
    TenSanPham NVARCHAR(200) NOT NULL,
    HangSanXuat NVARCHAR(100) NULL,
    HinhAnh NVARCHAR(MAX) NULL,   -- Link ảnh
    GiaBan DECIMAL(18,2) NOT NULL DEFAULT 0,   -- Giá niêm yết
    ThongSoKyThuat NVARCHAR(MAX) NULL,   -- Lưu dạng Key-Value: "RAM: 8GB \n Bus: 3200"
    ThoiGianBaoHanh INT NOT NULL DEFAULT 12,
    MaDanhMuc INT NOT NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_SanPham_DanhMuc FOREIGN KEY (MaDanhMuc) REFERENCES DanhMuc(MaDanhMuc),
    CONSTRAINT CHK_SanPham_GiaBan CHECK (GiaBan >= 0)
);
GO

-- 6. Bảng PhieuNhap (Import Order)

CREATE TABLE PhieuNhap (
    MaPhieuNhap INT IDENTITY(1,1) PRIMARY KEY,
    NgayNhap DATETIME NOT NULL DEFAULT GETDATE(),
    TongTien DECIMAL(18,2) NOT NULL DEFAULT 0,
    GhiChu NVARCHAR(MAX) NULL,
    MaNhaCungCap INT NOT NULL,
    MaNguoiDung INT NOT NULL,   -- Người nhập kho
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_PhieuNhap_NhaCungCap FOREIGN KEY (MaNhaCungCap) REFERENCES NhaCungCap(MaNhaCungCap),
    CONSTRAINT FK_PhieuNhap_NguoiDung FOREIGN KEY (MaNguoiDung) REFERENCES NguoiDung(MaNguoiDung),
    CONSTRAINT CHK_PhieuNhap_TongTien CHECK (TongTien >= 0)
);
GO

-- 7. Bảng PhieuXuat (Export Order/Invoice)

CREATE TABLE PhieuXuat (
    MaPhieuXuat INT IDENTITY(1,1) PRIMARY KEY,
    NgayXuat DATETIME NOT NULL DEFAULT GETDATE(),
    TenKhachHang NVARCHAR(100) NULL,
    SoDienThoaiKhach VARCHAR(20) NULL,
    TongTien DECIMAL(18,2) NOT NULL DEFAULT 0,
    MaNguoiDung INT NOT NULL,   -- Người bán hàng
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_PhieuXuat_NguoiDung FOREIGN KEY (MaNguoiDung) REFERENCES NguoiDung(MaNguoiDung),
    CONSTRAINT CHK_PhieuXuat_TongTien CHECK (TongTien >= 0)
);
GO

-- 8. Bảng SeriSanPham (Product Serial) - QUAN TRỌNG NHẤT
-- Quản lý từng món hàng vật lý.

CREATE TABLE SeriSanPham (
    MaSeri INT IDENTITY(1,1) PRIMARY KEY,
    SoSeri VARCHAR(100) NOT NULL,   -- Barcode/QR Code
    TrangThai INT NOT NULL DEFAULT 1, -- 1: Trong kho, 2: Đã bán, 3: Lỗi
    MaSanPham INT NOT NULL,
    MaPhieuNhap INT NULL,   -- Có thể Null nếu nhập dữ liệu cũ
    MaPhieuXuat INT NULL,   -- Null khi mới nhập, có giá trị khi đã bán
    IsDeleted BIT NOT NULL DEFAULT 0,

    CONSTRAINT UQ_SeriSanPham_SoSeri UNIQUE (SoSeri),
    CONSTRAINT CHK_Seri_TrangThai CHECK (TrangThai IN (1,2,3)),
    CONSTRAINT FK_SeriSanPham_SanPham FOREIGN KEY (MaSanPham) REFERENCES SanPham(MaSanPham),
    CONSTRAINT FK_SeriSanPham_PhieuNhap FOREIGN KEY (MaPhieuNhap) REFERENCES PhieuNhap(MaPhieuNhap),
    CONSTRAINT FK_SeriSanPham_PhieuXuat FOREIGN KEY (MaPhieuXuat) REFERENCES PhieuXuat(MaPhieuXuat)
);
GO

-- 9. Bảng ChiTietPhieuNhap (Import Detail)
-- Dùng để xác định số lượng nhập (N) -> sinh ra N dòng bên bảng SeriSanPham

CREATE TABLE ChiTietPhieuNhap (
    MaChiTietNhap INT IDENTITY(1,1) PRIMARY KEY,
    SoLuong INT NOT NULL,   -- Số lượng nhập
    DonGiaNhap DECIMAL(18,2) NOT NULL DEFAULT 0,
    MaPhieuNhap INT NOT NULL,
    MaSanPham INT NOT NULL,
    
    CONSTRAINT FK_ChiTietNhap_PhieuNhap FOREIGN KEY (MaPhieuNhap) REFERENCES PhieuNhap(MaPhieuNhap),
    CONSTRAINT FK_ChiTietNhap_SanPham FOREIGN KEY (MaSanPham) REFERENCES SanPham(MaSanPham),
    CONSTRAINT CHK_ChiTietNhap_SoLuong CHECK (SoLuong > 0),
    CONSTRAINT CHK_ChiTietNhap_DonGiaNhap CHECK (DonGiaNhap >= 0)
);
GO

-- 10. Bảng ChiTietPhieuXuat (Export Detail)
-- LƯU Ý QUAN TRỌNG: Liên kết với SeriSanPham, KHÔNG liên kết với SanPham

CREATE TABLE ChiTietPhieuXuat (
    MaChiTietXuat INT IDENTITY(1,1) PRIMARY KEY,
    GiaTien DECIMAL(18,2) NOT NULL DEFAULT 0,   -- Giá bán thực tế
    MaPhieuXuat INT NOT NULL,
    MaSeri INT NOT NULL,   -- Bán đích danh con nào
    
    CONSTRAINT FK_ChiTietXuat_PhieuXuat FOREIGN KEY (MaPhieuXuat) REFERENCES PhieuXuat(MaPhieuXuat),
    CONSTRAINT FK_ChiTietXuat_Seri FOREIGN KEY (MaSeri) REFERENCES SeriSanPham(MaSeri),
    CONSTRAINT CHK_ChiTietXuat_GiaTien CHECK (GiaTien >= 0)
);
GO

