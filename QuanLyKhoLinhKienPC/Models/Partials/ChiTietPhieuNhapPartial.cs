using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace QuanLyKhoLinhKienPC.Models;

[ModelMetadataType(typeof(ChiTietPhieuNhapMetadata))]
public partial class ChiTietPhieuNhap
{
}

public class ChiTietPhieuNhapMetadata
{
    [Required(ErrorMessage = "Vui lòng nhập số lượng!")]
    [Range(1, int.MaxValue, ErrorMessage = "Số lượng nhập phải lớn hơn 0!")]
    public int SoLuong { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập đơn giá nhập!")]
    [Range(0, double.MaxValue, ErrorMessage = "Đơn giá nhập không được âm!")]
    public decimal DonGiaNhap { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn phiếu nhập!")]
    public int MaPhieuNhap { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn sản phẩm!")]
    public int MaSanPham { get; set; }
}
