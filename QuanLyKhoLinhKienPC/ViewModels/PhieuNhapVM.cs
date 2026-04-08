using System.ComponentModel.DataAnnotations;

namespace QuanLyKhoLinhKienPC.ViewModels
{
    public class SeriImportItem
    {
        public int MaSanPham { get; set; }
        public string? TenSanPham { get; set; }
        public decimal DonGiaNhap { get; set; }
        public string? RawSeris { get; set; } // Dữ liệu Seri thô cách nhau bởi dòng mới
        public int SoLuong => ListSeris.Count;

        public List<string> ListSeris => string.IsNullOrWhiteSpace(RawSeris)
            ? new List<string>()
            : RawSeris.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                      .Select(s => s.Trim())
                      .Where(s => !string.IsNullOrEmpty(s))
                      .Distinct()
                      .ToList();
    }

    public class PhieuNhapVM
    {
        [Required(ErrorMessage = "Vui lòng chọn Nhà cung cấp")]
        public int MaNhaCungCap { get; set; }

        public string? GhiChu { get; set; }

        public List<SeriImportItem> Items { get; set; } = new List<SeriImportItem>();
    }
}
