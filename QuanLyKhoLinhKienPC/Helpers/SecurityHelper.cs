using System;
using BCrypt.Net;

namespace QuanLyKhoLinhKienPC.Helpers
{
    public static class SecurityHelper
    {
        // Hàm mã hóa mật khẩu (Hash)
        public static string HashPassword(string password)
        {
            // HashPassword tự động tạo Salt ngẫu nhiên và gộp chung vào chuỗi kết quả
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        // Hàm kiểm tra mật khẩu (Verify)
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            try
            {
                // So sánh mật khẩu gốc với chuỗi Hash lôi từ CSDL
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
