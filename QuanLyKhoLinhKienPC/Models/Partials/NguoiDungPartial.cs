using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc;

namespace QuanLyKhoLinhKienPC.Models;

[ModelMetadataType(typeof(NguoiDungMetadata))]
public partial class NguoiDung
{
    // Cột ảo dùng cho màn hình Tạo mới / Đổi mật khẩu
    [NotMapped]
    [Compare("MatKhau", ErrorMessage = "Mật khẩu xác nhận không khớp!")]
    public string XacNhanMatKhau { get; set; }
}

public class NguoiDungMetadata
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập!")]
    [MaxLength(50, ErrorMessage = "Tên đăng nhập tối đa 50 ký tự!")]
    public string TenDangNhap { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu!")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự!")]
    public string MatKhau { get; set; }

    [MaxLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự!")]
    public string HoTen { get; set; }

    [MaxLength(100, ErrorMessage = "Email tối đa 100 ký tự!")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng!")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn vai trò!")]
    public int MaVaiTro { get; set; }
}