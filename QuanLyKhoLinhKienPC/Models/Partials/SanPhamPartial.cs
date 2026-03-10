using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc;

namespace QuanLyKhoLinhKienPC.Models;

// 1. Gắn thuộc tính này để kết nối với class chứa thông báo lỗi bên dưới
[ModelMetadataType(typeof(SanPhamMetadata))]
public partial class SanPham
{
    // Cột ảo số lượng tồn
    [NotMapped]
    public int SoLuongTon { get; set; }
}

// 2. Class phụ này dùng để chứa các câu thông báo lỗi
public class SanPhamMetadata
{
    [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm!")]
    [MaxLength(200, ErrorMessage = "Tên sản phẩm tối đa 200 ký tự!")]
    public string TenSanPham { get; set; }

    [MaxLength(100, ErrorMessage = "Hãng sản xuất tối đa 100 ký tự!")]
    public string HangSanXuat { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập giá bán!")]
    [Range(0, double.MaxValue, ErrorMessage = "Giá bán không được âm!")]
    public decimal GiaBan { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập thời gian bảo hành!")]
    [Range(0, 120, ErrorMessage = "Thời gian bảo hành từ 0 đến 120 tháng!")]
    public int ThoiGianBaoHanh { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn danh mục!")]
    public int MaDanhMuc { get; set; }

    // Lưu ý: Chúng ta KHÔNG đặt [Required] cho HinhAnh ở đây. 
    // Vì khi "Chỉnh sửa" (Edit), người dùng có thể giữ nguyên ảnh cũ. 
    // Việc bắt lỗi phải có ảnh khi "Thêm mới" (Create) đã code bên Controller rồi.
}
