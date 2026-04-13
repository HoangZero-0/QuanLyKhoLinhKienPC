using System;

namespace QuanLyKhoLinhKienPC.ViewModels
{
    // ViewModel phụ trợ cho trang chủ
    public class HoatDongVM
    {
        public string? LoaiHoatDong { get; set; } // "Nhap" hoặc "Xuat"
        public string? NguoiThucHien { get; set; } // "A đã..."
        public string? MoTa { get; set; }
        public DateTime ThoiGian { get; set; }
        public string? Icon { get; set; }
        public string? ColorClass { get; set; }
        public string ThoiGianText
        {
            get
            {
                var ts = DateTime.Now - ThoiGian;
                if (ts.TotalMinutes < 60) return $"{(int)ts.TotalMinutes} phút trước";
                if (ts.TotalHours < 24) return $"{(int)ts.TotalHours} giờ trước";
                if (ts.TotalDays < 2) return "Hôm qua";
                return $"{(int)ts.TotalDays} ngày trước";
            }
        }
    }
}
