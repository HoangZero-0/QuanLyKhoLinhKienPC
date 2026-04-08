namespace QuanLyKhoLinhKienPC.Models
{
    public static class SeriStatus
    {
        public const int TrongKho = 1;
        public const int DaBan = 2;
        public const int LoiBaoHanh = 3;

        public static string GetStatusName(int status)
        {
            return status switch
            {
                TrongKho => "Trong kho",
                DaBan => "Đã bán",
                LoiBaoHanh => "Lỗi/Bảo hành",
                _ => "Không xác định"
            };
        }

        public static string GetStatusBadge(int status)
        {
            return status switch
            {
                TrongKho => "badge bg-success",
                DaBan => "badge bg-primary",
                LoiBaoHanh => "badge bg-danger",
                _ => "badge bg-secondary"
            };
        }
    }
}
