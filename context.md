# 1. TỔNG QUAN DỰ ÁN (PROJECT META)

- **Tên dự án:** QuanLyKhoLinhKienPC
- **Mục tiêu cốt lõi:** Quản lý kho linh kiện máy tính (PC Components Warehouse Management).
- **Nền tảng đích:** Web / ASP.NET Core MVC 8.0

# 2. TECH STACK (CÔNG NGHỆ SỬ DỤNG)

- **Ngôn ngữ chính:** C# (ASP.NET Core)
- **Frameworks/Libraries:**
  - ASP.NET Core MVC
  - Entity Framework Core (SQL Server)
  - Microsoft.AspNetCore.Authentication.Cookies (PCCookieAuth)
- **Database:** SQL Server (LocalDB/SQLEXPRESS)
- **Khác:** Windows OS

# 3. KIẾN TRÚC & CẤU TRÚC THƯ MỤC (ARCHITECTURE & STRUCTURE)

- **Mô hình kiến trúc:** MVC (Models-Views-Controllers)
- **Cấu trúc thư mục cốt lõi:**
  ```text
  /QuanLyKhoLinhKienPC
    /Controllers   # Chứa các Controller xử lý logic nghiệp vụ và điều hướng trang
    /Models        # Chứa EF Core Entities (Database Context) và ViewModels
    /Views         # Chứa các file .cshtml cho giao diện
    /wwwroot       # Thư mục chứa CSS, JS, hình ảnh sản phẩm
    /Helpers       # Các hàm bổ trợ (nếu có)
  ```

# 4. QUY CHUẨN MÃ NGUỒN (CODING CONVENTIONS)

- **Naming Convention (Đặt tên):**
  - Class/Method: PascalCase
  - Biến: camelCase
- **Error Handling:** Sử dụng try-catch trong các Controller action, thường trả về Error View.
- **Authentication:** Cookie Authentication (`PCCookieAuth`), yêu cầu đăng nhập toàn cục (`AuthorizeFilter`).
- **Authorization:** Phân quyền dựa trên `MaVaiTro` trong bảng `NguoiDung`.

# 5. NGHIỆP VỤ & THỰC THỂ CHÍNH (DOMAIN & CORE ENTITIES)

- **Thuật ngữ dự án (Glossary):**
  - `PhieuNhap`: Phiếu nhập kho linh kiện từ nhà cung cấp.
  - `PhieuXuat`: Phiếu xuất kho bán cho khách hàng.
  - `SeriSanPham`: Quản lý linh kiện theo số Seri (mỗi linh kiện có một mã seri riêng).
- **Các Model/Thực thể cốt lõi:**
  - `SanPham`: Thông tin chung về linh kiện (Tên, Hãng, Giá bán, Bảo hành).
  - `NguoiDung`: Thông tin nhân viên/quản lý hệ thống.
  - `DanhMuc`: Phân loại linh kiện (Mainboard, CPU, RAM, v.v.).
  - `PhieuNhap` / `ChiTietPhieuNhap`.
  - `PhieuXuat` / `ChiTietPhieuXuat`.

# 6. LUỒNG HOẠT ĐỘNG CHÍNH (CORE FLOWS)

## Flow 1: Đăng nhập & Phân quyền

- **Trigger:** Truy cập bất kỳ trang nào (Global Authorize) -> Redirect về `/Auth/Login`.
- **Logic:** Kiểm tra `TenDangNhap` và `MatKhau` trong bảng `NguoiDung`. Nếu đúng, tạo Cookie `PCCookieAuth`.

## Flow 2: Nhập hàng (Import)

- **Files chính:** `PhieuNhapController.cs`.
- **Logic:** Tạo `PhieuNhap`, thêm `ChiTietPhieuNhap`, và quản lý `SeriSanPham` tương ứng.

## Flow 3: Xuất hàng (Export)

- **Files chính:** `PhieuXuatController.cs`.
- **Logic:** Tạo `PhieuXuat`, thêm `ChiTietPhieuXuat`, và cập nhật trạng thái `SeriSanPham` đã bán.

# 7. TRẠNG THÁI DỰ ÁN & BACKLOG (STATE & TODO)

## Hoàn thành (Done)

- **Hệ thống & Tài khoản:**
  - [x] `Xác thực (Auth)`:
    - Đăng nhập (Xác thực mật khẩu mã hóa bằng **BCrypt**).
    - Đăng xuất (Xóa Cookie xác thực).
    - Quản lý Claims (Lưu User ID, Tên, Vai trò vào Cookie).
    - Bảo mật: Chống Open Redirect, Validate Anti-Forgery Token.
  - [x] `Trang chủ (Home - Dashboard)`:
    - Thống kê tổng hợp: Tổng doanh thu (từ phiếu chưa xóa), Tổng đơn hàng, Tổng mẫu mã sản phẩm, Tổng tồn kho thực tế (số Seri sẵn có).
  - [x] `Hồ sơ cá nhân (HoSo)`:
    - Xem thông tin cá nhân.
    - Cập nhật thông tin: Họ tên, Email.
    - Đổi mật khẩu: Kiểm tra mật khẩu cũ, mã hóa mật khẩu mới bằng **BCrypt**.
  - [x] `Quản lý Người dùng (NguoiDung)`:
    - Danh sách & Tìm kiếm (Họ tên, Tên đăng nhập).
    - Lọc theo Vai trò.
    - Chi tiết người dùng.
    - Thêm mới: Kiểm tra trùng Tên đăng nhập, mã hóa mật khẩu **BCrypt**.
    - Xóa mềm: Chốt chặn an toàn (Chống tự xóa tài khoản chính mình).
    - Thùng rác & Khôi phục tài khoản.
  - [x] `Quản lý Vai trò (VaiTro)`:
    - Danh sách & Tìm kiếm (Tên vai trò).
    - Chi tiết, Thêm mới, Chỉnh sửa vai trò.
    - Xóa mềm: Chốt chặn ràng buộc (Ngăn xoá nếu còn Người dùng hoạt động).
    - Thùng rác & Khôi phục.

- **Danh mục & Sản phẩm:**
  - [x] `Quản lý Danh mục (DanhMuc)`:
    - Danh sách & Tìm kiếm (Tên danh mục).
    - Chi tiết, Thêm mới, Chỉnh sửa thông tin.
    - Xóa mềm: Chốt chặn an toàn (Ngăn xoá nếu còn Sản phẩm hoạt động bên trong).
    - Thùng rác & Khôi phục.
  - [x] `Quản lý Sản phẩm (SanPham)`:
    - Danh sách & Tìm kiếm (Tên sản phẩm, Hãng sản xuất).
    - Lọc theo Danh mục và Hãng sản xuất.
    - Chi tiết sản phẩm: Tự động tính số lượng tồn thực tế từ kho Seri.
    - Thêm mới & Chỉnh sửa: Xử lý hình ảnh chuyên sâu (**ImageSharp**) -> Resize 800x800, lót nền trắng, chuyển định dạng **WebP**, tự động xóa file ảnh cũ khi cập nhật.
    - Xóa mềm: Chốt chặn thông minh (Chặn xoá khi còn hàng tồn Kho hoặc đang Bảo hành; cho phép ẩn nếu đã bán hết).
    - Thùng rác & Khôi phục.
  - [x] `Quản lý Nhà cung cấp (NhaCungCap)`:
    - Danh sách & Tìm kiếm (Tên nhà cung cấp, Số điện thoại).
    - Chi tiết, Thêm mới, Chỉnh sửa thông tin.
    - Xóa mềm, Thùng rác & Khôi phục.

- **Kho hàng & Nghiệp vụ:**
  - [x] `Quản lý Seri Sản phẩm (SeriSanPham)`:
    - Danh sách & Tìm kiếm (Số Seri, Tên sản phẩm, Hãng sản xuất).
    - Lọc theo Trạng thái (Tồn kho, Đã bán, Lỗi/Bảo hành).
    - Lọc theo Khoảng ngày (chọn ngày Nhập hoặc ngày Xuất).
    - Chi tiết truy vết: Xem lịch sử (Nhập từ NCC nào, ngày nào, giá nào; Bán cho ai, ngày nào, giá nào).
    - Quản lý Bảo hành: Chuyển trạng thái Lỗi/Bảo hành, tự động đối soát thời hạn bảo hành từ ngày bán.
    - Xóa mềm: Chốt chặn an toàn (Chặn xoá nếu mã máy đã bán hoặc đang bảo hành).
    - Thùng rác & Khôi phục.
    - In thống kê thông tin đã lọc
  - [x] `Nhập kho (PhieuNhap)`:
    - Danh sách phiếu (Xem NCC, Người nhập, Ngày nhập).
    - Chi tiết phiếu: Hiển thị danh sách sản phẩm và các mã Seri tương ứng đã sinh.
    - Thêm mới (Create): Giao diện JS thêm dòng sản phẩm, tự động tính tổng tiền, **Tự động sinh mã Seri** ngẫu nhiên theo quy tắc.
    - Xóa mềm: Chốt chặn an toàn (Chặn xoá nếu có mã máy đã Bán/Bảo hành) + Cascade đồng bộ ẩn các Seri liên quan.
    - Thùng rác & Khôi phục: Đồng bộ khôi phục cả Phiếu và các Seri con.
    - Bảo mật: Sử dụng **Transaction** cấp cơ sở dữ liệu để bảo vệ tính toàn vẹn khi sinh Seri hàng loạt.
  - [x] `Xuất kho (PhieuXuat)`:
    - Danh sách phiếu (Xem khách hàng, SĐT, Người lập, Ngày xuất).
    - Chi tiết phiếu: Hiển thị danh sách Seri đã bán.
    - Thêm mới (Create): Giao diện JS, hiển thị tồn kho thực tế, **Tự động rút Seri từ kho** theo số lượng bán.
    - Xóa mềm: Cascade hoàn trả Seri về kho (Trạng thái 1) và gỡ liên kết phiếu xuất.
    - Thùng rác & Khôi phục: Rút lại Seri từ kho (Chốt chặn: Chặn khôi phục nếu hàng đã bán lại cho khách khác).
    - Bảo mật: Sử dụng **Transaction** bảo vệ luồng rút/trả kho.
  - [x] `Cảnh báo tồn kho & Hoạt động`: Module Dashboard hiện thống kê cảnh báo hết hàng (khi số seri < 3) và hiển thị hoạt động nhập xuất gần đây.

## Đang tiến hành (In Progress)

- [ ] `Tối ưu UI/UX`: Chỉnh sửa lại nhập, xuất toàn diện từ logic đến giao diện.
- [ ] `Báo cáo & In ấn`: Phát triển chức năng xuất file PDF/Excel cho hóa đơn Nhập và Xuất, báo cáo doanh thu, báo cáo tồn kho (Excel: Dùng cho Nhập kho và Báo cáo thống kê. Dữ liệu nhập cần bóc tách nhiều cột (Header Auto-Detection, bóc tách Seri, làm sạch đơn giá). Thống kê cần Excel để kế toán/chủ shop có thể filter, tính toán lại, PDF: Dùng cho Phiếu Xuất (Hóa đơn bán hàng). Hóa đơn đưa cho khách hàng cần sự nguyên bản, không thể chỉnh sửa, in ra đẹp mắt và chuyên nghiệp).
- [ ] `Mối liên hệ Giá cả`: Giá nhập (trong bảng ChiTietPhieuNhap) sẽ ảnh hưởng đến giá bán chung của SanPham. Tuy nhiên, không được tự động đè giá. Giải pháp dễ nhất: Khi lập Phiếu Nhập, hiển thị giá nhập hiện tại, hệ thống gợi ý cập nhật GiaBan bên bảng SanPham, nhưng quyền quyết định cuối cùng vẫn thuộc về nhân viên nhập kho.

## Cần làm (TODO/Backlog)

- [ ] Kiểm tra và xử lý triệt để các lỗi logic phát sinh trong quá trình vận hành thực tế.
- [ ] Tối ưu hóa UI/UX cho các trang báo cáo thống kê.
- [ ] Kiểm tra và xử lý các lỗi logic phát sinh.
