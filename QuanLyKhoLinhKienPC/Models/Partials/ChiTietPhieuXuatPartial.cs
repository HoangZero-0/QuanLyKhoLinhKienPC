using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace QuanLyKhoLinhKienPC.Models;

[ModelMetadataType(typeof(ChiTietPhieuXuatMetadata))]
public partial class ChiTietPhieuXuat
{
}

public class ChiTietPhieuXuatMetadata
{
    [Required(ErrorMessage = "Vui lòng nhập giá tiền!")]
    [Range(0, double.MaxValue, ErrorMessage = "Giá bán không được âm!")]
    public decimal GiaTien { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn phiếu xuất!")]
    public int MaPhieuXuat { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn số serial!")]
    public int MaSeri { get; set; }
}
