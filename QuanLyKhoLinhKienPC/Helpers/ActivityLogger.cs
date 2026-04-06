using QuanLyKhoLinhKienPC.Models;
using System.Threading.Tasks;
using System;

namespace QuanLyKhoLinhKienPC.Helpers
{
    public static class ActivityLogger
    {
        /// <summary>
        /// Ghi nhận lịch sử hoạt động vào bảng NhatKyHoatDong
        /// </summary>
        /// <param name="context">DbContext của Controller hiện tại</param>
        /// <param name="maNguoiDung">ID nhân viên thực hiện (nếu có)</param>
        /// <param name="loaiHanhDong">Phân loại hành động: (Thêm, Sửa, Xóa, Khôi phục)</param>
        /// <param name="doiTuong">Thực thể bị tác động (Sản Phẩm, Phiếu Nhập...)</param>
        /// <param name="moTaChiTiet">Thông tin miêu tả (A đã thêm sản phẩm ABC)</param>
        public static async Task LogAsync(
            QuanLyKhoLinhKienPCContext context,
            int maNguoiDung,
            string loaiHanhDong,
            string doiTuong,
            string moTaChiTiet)
        {
            try
            {
                var nhatKy = new NhatKyHoatDong
                {
                    MaNguoiDung = maNguoiDung,
                    LoaiHanhDong = loaiHanhDong,
                    DoiTuong = doiTuong,
                    MoTaChiTiet = moTaChiTiet,
                    ThoiGian = DateTime.Now
                };

                context.NhatKyHoatDong.Add(nhatKy);
                await context.SaveChangesAsync();
            }
            catch (Exception)
            {
                // Swallow error safely để không làm sập tiến trình nghiệp vụ chính do lỗi log.
                // Trong thực tế có thể log exception ra file ILogger ở đây.
            }
        }
    }
}
