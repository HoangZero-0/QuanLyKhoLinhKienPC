using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace QuanLyKhoLinhKienPC.Models;

[ModelMetadataType(typeof(SeriSanPhamMetadata))]
public partial class SeriSanPham
{
}

public class SeriSanPhamMetadata
{
    [Required(ErrorMessage = "Vui lòng nhập số Serial!")]
    [MaxLength(100, ErrorMessage = "Số Serial tối đa 100 ký tự!")]
    public string SoSeri { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập trạng thái!")]
    [Range(1, 3, ErrorMessage = "Trạng thái không hợp lệ (1: Trong kho, 2: Đã bán, 3: Lỗi)!")]
    public int TrangThai { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn sản phẩm!")]
    public int MaSanPham { get; set; }
}
