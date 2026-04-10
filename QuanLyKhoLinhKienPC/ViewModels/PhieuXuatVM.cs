using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLyKhoLinhKienPC.ViewModels
{
    public class PhieuXuatVM
    {
        [Required(ErrorMessage = "Vui lòng nhập tên khách hàng")]
        public string TenKhachHang { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [RegularExpression(@"^(?!0+$)(\+\d{1,3}[- ]?)?(?!0+$)\d{10,11}$", ErrorMessage = "Vui lòng nhập đúng định dạng số điện thoại (10-11 số)!")]
        public string SoDienThoaiKhach { get; set; } = string.Empty;



        // Danh sách các mục xuất kho
        public List<XuatKhoItemVM> Items { get; set; } = new List<XuatKhoItemVM>();
    }

    public class XuatKhoItemVM
    {
        public int MaSanPham { get; set; }
        public decimal DonGiaXuat { get; set; }

        // Danh sách các MaSeri được chọn cho mặt hàng này (từ Select2)
        public List<int> SelectedSeriIds { get; set; } = new List<int>();

        // Thuộc tính hỗ trợ hiển thị
        public string? TenSanPham { get; set; }
    }
}
