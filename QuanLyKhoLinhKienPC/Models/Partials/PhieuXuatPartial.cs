using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace QuanLyKhoLinhKienPC.Models;

[ModelMetadataType(typeof(PhieuXuatMetadata))]
public partial class PhieuXuat
{
}

public class PhieuXuatMetadata
{
    [Required(ErrorMessage = "Vui lòng nhập ngày xuất!")]
    public DateTime NgayXuat { get; set; }

    [MaxLength(100, ErrorMessage = "Tên khách hàng tối đa 100 ký tự!")]
    public string TenKhachHang { get; set; }

    [MaxLength(20, ErrorMessage = "Số điện thoại khách tối đa 20 ký tự!")]
    public string SoDienThoaiKhach { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tổng tiền!")]
    [Range(0, double.MaxValue, ErrorMessage = "Tổng tiền không được âm!")]
    public decimal TongTien { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn người dùng bán hàng!")]
    public int MaNguoiDung { get; set; }
}
