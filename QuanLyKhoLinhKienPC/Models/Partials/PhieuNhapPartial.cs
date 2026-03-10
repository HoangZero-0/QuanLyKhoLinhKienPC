using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace QuanLyKhoLinhKienPC.Models;

[ModelMetadataType(typeof(PhieuNhapMetadata))]
public partial class PhieuNhap
{
}

public class PhieuNhapMetadata
{
    [Required(ErrorMessage = "Vui lòng nhập ngày nhập!")]
    public DateTime NgayNhap { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tổng tiền!")]
    [Range(0, double.MaxValue, ErrorMessage = "Tổng tiền không được âm!")]
    public decimal TongTien { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn nhà cung cấp!")]
    public int MaNhaCungCap { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn người dùng đăng nhập!")]
    public int MaNguoiDung { get; set; }
}
