using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyKhoLinhKienPC.Models
{
    [Table("NhatKyHoatDong")]
    public class NhatKyHoatDong
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaNhatKy { get; set; }

        public int MaNguoiDung { get; set; }

        [Required]
        [StringLength(50)]
        public string? LoaiHanhDong { get; set; }

        [Required]
        [StringLength(100)]
        public string? DoiTuong { get; set; }

        public string? MoTaChiTiet { get; set; }

        public DateTime? ThoiGian { get; set; }

        [ForeignKey("MaNguoiDung")]
        public virtual NguoiDung? MaNguoiDungNavigation { get; set; }
    }
}
