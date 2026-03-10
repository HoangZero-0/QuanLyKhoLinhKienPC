using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace QuanLyKhoLinhKienPC.Models;

[ModelMetadataType(typeof(NhaCungCapMetadata))]
public partial class NhaCungCap { }

public class NhaCungCapMetadata
{
    [Required(ErrorMessage = "Vui lòng nhập tên nhà cung cấp!")]
    [MaxLength(200, ErrorMessage = "Tên nhà cung cấp tối đa 200 ký tự!")]
    public string TenNhaCungCap { get; set; }

    [MaxLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự!")]
    [RegularExpression(@"^(?!0+$)(\+\d{1,3}[- ]?)?(?!0+$)\d{10,11}$|^$", ErrorMessage = "Vui lòng nhập đúng định dạng số điện thoại hoặc để trống!")]
    public string SoDienThoai { get; set; }

    [Display(Name = "Địa chỉ")]
    public string DiaChi { get; set; }
}