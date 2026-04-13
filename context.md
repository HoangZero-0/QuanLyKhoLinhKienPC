# 1. TỔNG QUAN DỰ ÁN (PROJECT META)

- **Tên dự án:** QuanLyKhoLinhKienPC
- **Mục tiêu cốt lõi:** Quản lý kho linh kiện máy tính (PC Components Warehouse Management) với các quy trình nhập, xuất, bảo hành và theo dõi vòng đời sản phẩm chi tiết qua mã Seri riêng biệt.
- **Nền tảng đích:** Web / ASP.NET Core MVC 8.0

# 2. TECH STACK (CÔNG NGHỆ SỬ DỤNG)

- **Ngôn ngữ chính:** C# 12 (ASP.NET Core 8.0)
- **Frameworks/Libraries:**
  - ASP.NET Core MVC
  - Entity Framework Core (SQL Server)
  - Microsoft.AspNetCore.Authentication.Cookies (PCCookieAuth)
  - SixLabors.ImageSharp (Xử lý hình ảnh nâng cao, padding khung, chuyển đổi định dạng WebP)
  - BCrypt.Net-Next (Mã hóa mật khẩu 1 chiều an toàn)
  - ClosedXML (Xuất/Nhập dữ liệu theo chuẩn file Excel)
  - SweetAlert2 (Bộ giao diện thông báo Popup và Confirm hiện đại)
- **Database:** SQL Server
- **Khác:** Windows OS

# 3. KIẾN TRÚC & CẤU TRÚC THƯ MỤC (ARCHITECTURE & STRUCTURE)

- **Mô hình kiến trúc:** Chuẩn thiết kế MVC (Models - Views - Controllers)
- **Cấu trúc thư mục cốt lõi:**
  ```text
  /QuanLyKhoLinhKienPC
    /Controllers   # Các Controller xử lý logic nghiệp vụ, giao tiếp Model - View và bắt luồng lỗi.
    /Models        # Entity Framework Core Entities (DB First), ViewModels (PhieuNhapVM, PhieuXuatVM, HoatDongVM)
    /Views         # Giao diện .cshtml (Razor), tích hợp cấu trúc Glassmorphism cao cấp
    /wwwroot       # CSS tĩnh, script JS, font icon, và thư mục lưu trữ vật lý /images/sanpham/
    /Helpers       # Chứa SecurityHelper (Mã hóa/Kiểm tra Auth), ActivityLogger (Ghi nhật ký hệ thống Async)
  ```

# 4. QUY CHUẨN MÃ NGUỒN (CODING CONVENTIONS)

- **Naming Convention (Đặt tên):**
  - Class/Method: PascalCase
  - Biến cục bộ, tham số: camelCase
- **Error Handling:** Try-catch trong các Controller action, có Exception Handler (bộ bắt lỗi) toàn cục mapping đến `/Home/Error`.
- **Authentication:** Cookie Authentication (`PCCookieAuth`), yêu cầu đăng nhập toàn cục thông qua cấu hình `AuthorizeFilter` (Chặn đứng toàn bộ truy cập hệ thống ở phía Client nếu chưa đăng nhập).
- **Authorization (RBAC):** Phân quyền nghiêm ngặt dựa trên vai trò (`[Authorize(Roles = ...)]` ở Controller) và Render ẩn/hiện Layout chủ động (ở View).
- **View Safety:** Sử dụng `ViewData/TempData` làm chuẩn truyền tải dữ liệu Alert. Form sử dụng `[ValidateAntiForgeryToken]` chặn tấn công XSS/CSRF và kĩ thuật Anti-Overposting (chỉ Bind và Update các Record thiết yếu, loại bỏ việc tiêm mã độc qua Network Payload).
- **Transaction:** Bất cứ thao tác DB liên hoàn phức tạp nào (Lưu phiếu xuất/nhập, Tự động phân rã tạo Seri) đều sử dụng `IDbContextTransaction` để cấu trúc tính toàn vẹn dữ liệu (ACID). Nếu 1 truy vấn fail, toàn bộ phiếu và seri sẽ được Rollback tức thời.

# 5. NGHIỆP VỤ & THỰC THỂ CHÍNH (DOMAIN & CORE ENTITIES)

- **Thuật ngữ (Glossary):**
  - `PhieuNhap`: Cấu trúc phiếu nhập kho từ Nhà Cung Cấp. Sinh tự động hàng vạn mã Seri.
  - `PhieuXuat`: Cấu trúc xuất kho bán lẻ cho khách (Hóa đơn). Khóa Seri tự động và kiểm thử tồn kho.
  - `SeriSanPham`: Định danh mã quét độc nhất cho từng linh kiện vật lý. Mắt xích quan trọng nhất để theo dõi lịch trình vòng đời thiết bị.
- **Các Model/Thực thể cốt lõi:**
  - `SanPham`, `DanhMuc`, `NhaCungCap`.
  - `NguoiDung`, `VaiTro`, `NhatKyHoatDong`.
  - `PhieuNhap` & `ChiTietPhieuNhap`.
  - `PhieuXuat` & `ChiTietPhieuXuat`.

# 6. LUỒNG HOẠT ĐỘNG CHÍNH (CORE FLOWS)

## Flow 1: Đăng nhập, Bảo mật & Logging ngầm (Global Safety Track)

- Xác thực User Identity bằng CookieAuth và Claims.
- Mọi thao tác Thêm/Sửa/Xóa/Khôi phục trong Admin-Site đều được chạy ngầm thông qua `ActivityLogger.LogAsync` để quản trị theo thời gian thực tại màn hình Trang Chủ (Take 30).

## Flow 2: Sinh trưởng Hình Ảnh Cơ Chế Canvas (Image Optimizer)

- Khi Thêm/Sửa sản phẩm: Máy chủ không tốn băng thông lưu trữ độ phân giải RAW. `ImageSharp` sẽ scale ảnh tối đa 800x800, chèn Canvas trắng vào ảnh đuôi .PNG để chống viền đen, sau đó Nén Lossy xuống `.webp`. File rác (ảnh cũ) bị trigger tự động xóa khỏi Local-Storage để tiết kiệm ổ cứng.

## Flow 3: Máy trạng thái Thời gian thực linh kiện (Seri Lifecycle Tracker)

- **Trạng Thái 1 (Tồn kho):** Nhập mới tự sinh. Lấy mốc cấu trúc bằng `NgayNhap`.
- **Trạng thái 2 (Đã bán):** Khi Hóa Đơn Xuất kích hoạt, rút seri từ State 1. Lấy mốc bằng `NgayXuat` để tạo Checkpoint cho Chế độ Bảo hành.
- **Trạng thái 3 (Lỗi/Bảo hành):** Có Cảnh báo Lỗi Hết Hạn tự động. Khi khách mang Seri tới báo hỏng, Máy tự Lookup bằng ID về ChiTietPhieuXuat để nội suy `Ngày Mua` + `Hạn Của Máy (Tháng)`, tự động nhả TempData là máy này MIỄN PHÍ hay DỊCH VỤ TÍNH TIỀN.

## Flow 4: Ghost-Recovery (Cơ chế Phục Sinh Dữ Liệu Rác)

- Khi `Phiếu Xuất` bị Hủy (Xóa mềm), các `Mã Seri` không còn trỏ về Hóa Đơn Khách Hàng. Chúng trở thành Máy Còn Kho.
- Khi Click Nút "Khôi phục Hóa Đơn", thuật toán xuất sắc dùng `ChiTietPhieuXuat` (vốn được giữ nguyên trong thùng rác) làm lớp Proxy Table nội suy truy vết tìm và khóa các Seri lúc nãy trở ngược lại thành "Đã Bán". Nếu trong thời gian nằm Thùng Rác, nhân sự lầm tưởng mã Seri đó còn Kho và Đem Bán Lần Nữa (MaPhieuXuat != Null) thì hệ thống chốt chặn "Cấm Khôi Phục Hóa Đơn".

# 7. MA TRẬN PHÂN QUYỀN MỞ RỘNG (RBAC CORE MATRIX)

Dựa trên thiết kế kiến trúc toàn diện của dự án, 3 nhóm quyền chính thức được cấp phát như sau:

## 7.1. Nguyên Tắc Phân Quyền Chung

- **Admin (Quản trị viên):** Toàn quyền hệ thống, định đoạt nhân sự, cấu hình vai trò.
- **Nhân viên kho:** Nắm giữ chìa khóa đối lưu vật lý tồn kho. Nhập kho, tạo lô Seri, xóa mềm và khôi phục (Restore) linh kiện từ giỏ rác.
- **Nhân viên bán hàng:** Chỉ có quyền sinh hóa đơn Đầu Ra. Được khôi phục (Restore) hóa đơn xuất nhưng cấm chỉnh sửa thông tin gốc sản phẩm/seri của công ty.

## 7.2. Bảng Chức Năng (11 Module Lõi)

| Chức Năng (Controller) | Tác Vụ                          | Admin | Kho | Sale | Ghi chú                        |
| ---------------------- | ------------------------------- | :---: | :-: | :--: | ------------------------------ |
| **Hệ thống**           | Login, Hồ Sơ, Dashboard         |  ✅   | ✅  |  ✅  | Quyền tối thiểu                |
| **Nhân sự**            | NguoiDung, VaiTro               |  ✅   | ❌  |  ❌  | Cấm đụng cấu hình Core         |
| **Danh Mục & NCC**     | Xem, Tìm kiếm                   |  ✅   | ✅  |  ✅  | Sale cần xem để tư vấn         |
|                        | Tạo, Sửa, Xóa (Soft), Khôi phục |  ✅   | ✅  |  ❌  | Kho quản lý Master Data        |
| **Sản Phẩm**           | Xem, Tìm kiếm                   |  ✅   | ✅  |  ✅  | Sale xem tồn kho ảo            |
|                        | Tạo, Sửa, Xóa (Soft), Khôi phục |  ✅   | ✅  |  ❌  | Kho nhập hàng lên kệ           |
| **Nhập Kho**           | Xem, Tìm kiếm                   |  ✅   | ✅  |  ✅  |                                |
|                        | Tạo phiếu, Sửa, Xóa, Khôi phục  |  ✅   | ✅  |  ❌  | _Nghiệp vụ tuyệt đối của Kho_  |
| **Xuất Kho (Bán)**     | Xem, Tìm kiếm                   |  ✅   | ✅  |  ✅  |                                |
|                        | Tạo hóa đơn, Sửa, Xóa, Restore  |  ✅   | ❌  |  ✅  | _Nghiệp vụ tuyệt đối của Sale_ |
| **Seri**               | Xem, Tìm kiếm, Truy vết lỗi     |  ✅   | ✅  |  ✅  | Sale báo giá đổi trả           |
|                        | Xóa thủ công, Restore Seri      |  ✅   | ✅  |  ❌  |                                |

## 7.3. Bảng Báo Cáo & In Ấn (Double-Layer Secured)

| Tính năng Tương tác          | Admin | Kho | Sale | Giải thích nghiệp vụ             |
| ---------------------------- | :---: | :-: | :--: | -------------------------------- |
| **1. In Phiếu Xuất (Khách)** |  ✅   | ❌  |  ✅  | Sale in bill. Kho chỉ nhặt hàng. |
| **2. In Phiếu Nhập (NCC)**   |  ✅   | ✅  |  ❌  | Kho in tài liệu lưu hồ sơ.       |
| **3. In DS Seri**            |  ✅   | ✅  |  ❌  | Kho in giấy xách đi đối soát.    |
| **4. Xuất Excel DS Seri**    |  ✅   | ✅  |  ❌  | Kho xuất file kiểm kê.           |
| **5. Xuất Excel NXT**        |  ✅   | ✅  |  ❌  | Kho chốt sổ Kế toán cuối tháng.  |
| **6. Excel Doanh Thu**       |  ✅   | ❌  |  ❌  | **TUYỆT MẬT.** Độc quyền Admin.  |

# 8. TRẠNG THÁI DỰ ÁN & BACKLOG (STATE & TODO)

## Hoàn thành (Done)

- [x] **Hệ thống & Phân Quyền:** Hoàn thiện AuthFilter, Thuật toán Hashing BCrypt, RBAC Roles cho toàn bộ ứng dụng C# MVC. Tự động hóa ActivityLogger bắt toàn bộ log sự kiện User thao tác hệ thống.
- [x] **UI/UX Toàn Cục (Refactored):** Thiết kế Layout dạng Glassmorphism, đổ bóng Vector. Chuẩn hóa thao tác người dùng bằng Cấu trúc thẻ Toast & Xác nhận từ thư viện SweetAlert2 toàn cục. Đã làm sạch toàn bộ Rác Render HTML `@if()` bị tồn đọng và thừa thãi.
- [x] **Module Danh Mục & Nhà Cung Cấp:** Refactor cấu trúc CRUD mượt mà. Triệt tiêu xóa thực, tích hợp thùng rác có logic ràng buộc chéo.
- [x] **Module Sản Phẩm:** Triển khai bộ nhúng ImageSharp WebP Pipeline siêu tối ưu. Tính tồn kho ảo bằng đếm Query Async thông minh.
- [x] **Module Nhân Viên & Vai Trò:** Ràng buộc tự xóa bản quyền, ngăn chặn xóa vai trò khi vẫn có Member tham gia nhóm đó.
- [x] **Module Nhập Kho & Xuất Kho:** Tích hợp giao diện Front-end Javascript động theo dòng. Gói gọn mã vòng lặp sinh Phiếu và Seri bằng IDbContextTransaction. Chống Over-posting tuyệt đối trên form Edit. Cấu trúc Proxy Restoring (Khôi phục thông qua thực thể trung gian) hoàn hảo cho việc hồi sinh Hóa đơn.
- [x] **Module Seri Sản Phẩm:** Giao diện Timeline Tracking sang trọng đẳng cấp để xem hành trình 1 chiếc máy. Truy vấn khoảng ngày phức hợp logic bằng Status Matrix. Tự tính ra "Hạn Bảo Hành Còn Không" với Sai số Millisecond = 0.
- [x] **Hệ thống Báo Cáo Doanh Nghiệp (Excel Reporting):** Triển khai bộ 3 báo cáo cốt lõi bằng `ClosedXML.Excel`: (1) Kiểm kê Seri, (2) Doanh thu & Lợi nhuận (truy vết giá vốn từ Seri), (3) Nhập - Xuất - Tồn. Toàn bộ báo cáo tuân thủ styling chuyên nghiệp, hỗ trợ lọc theo thời gian và trạng thái.
- [x] **Double-Layer RBAC Security (Bảo vệ dữ liệu tuyệt mật):** Khóa cứng 3 tuyến Báo Cáo Excel và 3 nút In Ấn PDF bằng ma trận quyền chuẩn doanh nghiệp: Khóa mức Controller `[Authorize(Roles="...")]` chống Bypass API, và tự động ẩn Elements trên mức Razor Views `@if(User.IsInRole(...))`. Admin toàn quyền, Sale bị chặn khỏi Báo Cáo Lợi Nhuận, Kho chỉ thấy luồng Hàng Hóa.
- [x] **Chốt Kiểm Toán Toàn Diện Hệ Thống Lõi C# (Technical Audit Passed):** Sạch rác 100%, Dependency sạch, API Mapping/Validation không rò rỉ ngoại lệ chưa bắt (Unhandled Exception). Toàn bộ Database Constraints hoạt động đồng bộ với MVC Level Constraints! Mọi File dư thừa đã được phân tích.

## Đang tiến hành (In Progress)

## Cần làm (TODO / Backlog)
