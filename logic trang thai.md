# TÀI LIỆU ĐẶC TẢ NGHIỆP VỤ KHO LÕI (CORE INVENTORY SPECIFICATION)

## I. Các trạng thái cơ bản (Status Codes)

Trong hệ thống, mỗi Số Seri (linh kiện đơn lẻ) sẽ luôn thuộc một trong ba trạng thái sau:

- **Trạng thái 1 (Trong kho):** Linh kiện sẵn sàng để bán.
- **Trạng thái 2 (Đã bán):** Linh kiện đã được xuất cho khách hàng theo hóa đơn.
- **Trạng thái 3 (Lỗi/Bảo hành):** Linh kiện đang gặp sự cố kỹ thuật hoặc đang trong quá trình sửa chữa.

## II. Sơ đồ chuyển đổi trạng thái (State Transition Diagram)

- **1 → 2:** Thông qua luồng Lập Phiếu Xuất.
- **2 → 1:** Thông qua luồng Hủy Phiếu Xuất (Hoàn kho).
- **1 → 3:** Báo lỗi hàng tồn kho.
- **2 → 3:** Tiếp nhận bảo hành từ khách hàng.
- **3 → 1:** Sửa xong hàng tồn (trả về kho).
- **3 → 2:** Sửa xong hàng khách (trả về trạng thái Đã bán).

## III. Chi tiết các luồng nghiệp vụ gây đổi trạng thái

### A. Luồng Nhập/Xuất Kho (Inventory Flow)

1. **Tạo Phiếu Nhập (PhieuNhap/Create):**
   - Hệ thống tự động sinh Số Seri/nhân viên nhập seri và gán mặc định `TrangThai = 1`.
2. **Tạo Phiếu Xuất (PhieuXuat/Create):**
   - Hệ thống quét các Seri của sản phẩm đó có `TrangThai = 1`.
   - Chuyển `TrangThai` từ `1 → 2`.
   - Gán `MaPhieuXuat` cho Seri đó để truy vết khách hàng sau này.
3. **Xóa Phiếu Xuất (PhieuXuat/DeleteConfirmed):**
   - Khi xóa một hóa đơn, hệ thống tự động tìm các Seri thuộc hóa đơn đó.
   - Chuyển `TrangThai` từ `2 → 1` và gán `MaPhieuXuat = null` (Hoàn trả linh kiện về kho).

### B. Luồng Bảo Hành & Báo Lỗi (SeriSanPham/ToggleDefect)

- **Trường hợp 1 (Báo lỗi hàng tồn):**
  - Nếu Seri đang ở kho (`TrangThai = 1`) → Chuyển sang `3`.
  - Mục đích: Loại bỏ linh kiện lỗi khỏi danh sách có thể bán.
- **Trường hợp 2 (Tiếp nhận bảo hành):**
  - Nếu Seri đã bán (`TrangThai = 2`) → Chuyển sang `3`.
  - Logic kiểm tra: Hệ thống so sánh ngày hiện tại với `Ngày xuất + Thời gian bảo hành`.
  - Nếu còn hạn: Chấp nhận bảo hành miễn phí.
  - Nếu hết hạn: Cảnh báo và chuyển sang sửa chữa dịch vụ.
- **Trường hợp 3 (Xử lý xong):**
  - Khi Seri đang ở trạng thái lỗi (`TrangThai = 3`) và nhấn xử lý xong:
  - Nếu `MaPhieuXuat == null`: Chuyển về `1` (Trả về kho để bán tiếp).
  - Nếu `MaPhieuXuat != null`: Chuyển về `2` (Trả lại cho khách hàng đã mua).

## IV. Nguyên lý điều hướng tự động

### 1. Sự ràng buộc giữa Trạng thái và "Mã chứng từ" (Document Linkage)

Đây là logic quan trọng nhất để hệ thống tự động hóa được việc chuyển trạng thái:

- **Trạng thái 1 (Trong kho):** Bắt buộc `MaPhieuXuat` phải bằng **Null**.
- **Trạng thái 2 (Đã bán):** Bắt buộc `MaPhieuXuat` phải **Khác Null**. Đây là "sợi dây" liên kết Seri với khách hàng.
- **Trạng thái 3 (Lỗi/Bảo hành):** Đây là trạng thái "tạm dừng". Hệ thống giữ nguyên giá trị `MaPhieuXuat` (nếu có) để biết máy này của ai khi sửa xong.

### 2. Logic "Rẽ nhánh tự động" khi thoát khỏi Trạng thái 3

Khi nhấn "Xử lý xong" tại Trạng thái 3:

- Hệ thống kiểm tra: _"Seri này có gắn với Phiếu Xuất nào không?"_
- Nếu **KHÔNG** (`MaPhieuXuat == null`): Tự động về **1** (Tồn kho).
- Nếu **CÓ** (`MaPhieuXuat != null`): Tự động về **2** (Đã bán).
  - _Lợi ích:_ Giúp nhân viên không cần phải nhớ máy này là hàng mới bị lỗi hay hàng khách mang đến bảo hành.

### 3. Trạng thái "Khả dụng" (Availability Definition)

Một Seri được coi là "Có thể bán" (Sellable) **CHỈ KHI** thỏa mãn 2 điều kiện cùng lúc:

- **Trạng thái == 1**
- **VÀ IsDeleted == false** (Không nằm trong thùng rác/bị hủy phiếu nhập).

## V. Các ràng buộc bảo vệ dữ liệu (Data Integrity)

- **Kiểm tra khi Khôi phục Phiếu Xuất:** Nếu một hóa đơn đã xóa muốn khôi phục lại, hệ thống sẽ kiểm tra xem các Seri cũ có còn ở trạng thái 1 (Trong kho) hay không. Nếu Seri đã bị bán cho khách khác, hệ thống sẽ **Chặn** không cho khôi phục để tránh tranh chấp dữ liệu.
- **Database Transaction:** Mọi thao tác đổi trạng thái đi kèm cập nhật Phiếu nhập/xuất luôn nằm trong một Transaction. Nếu một bước thất bại, toàn bộ sẽ rollback.
- **Ràng buộc Xóa dây chuyền (Cascading Soft Delete):** Khi một Phiếu Nhập bị xóa mềm, hệ thống tự động chuyển toàn bộ Seri thuộc phiếu đó sang `IsDeleted = true` để đảm bảo khớp dữ liệu kho.
- **Chốt chặn xóa (Safety Locks):**
  - **Chặn xóa Phiếu Nhập:** Nếu phiếu có bất kỳ Seri nào ở trạng thái 2 (Đã bán) hoặc 3 (Đang bảo hành).
  - **Chặn xóa Sản phẩm/Danh mục:** Nếu sản phẩm vẫn còn hàng trong kho (Thái 1) hoặc đang xử lý lỗi (Thái 3).
