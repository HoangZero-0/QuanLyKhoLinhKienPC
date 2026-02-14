using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKhoLinhKienPC.Models;

public partial class SanPham
{
    // Cột ảo số lượng tồn
    [NotMapped]
    public int SoLuongTon { get; set; }
}