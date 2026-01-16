using BCrypt.Net; // Thư viện mã hóa
using Microsoft.EntityFrameworkCore;
using PoliticsQuizApp.Data;
using PoliticsQuizApp.Data.Models; // Models vừa sinh ra
using System.Linq;

namespace PoliticsQuizApp.WPF.Services
{
    public class AuthService
    {
        private readonly PoliticsQuizDbContext _context;

        public AuthService()
        {
            _context = new PoliticsQuizDbContext();
        }

        // Hàm này kiểm tra user/pass
        public User Login(string username, string password)
        {
            // 1. Tìm user trong DB theo username
            var user = _context.Users.FirstOrDefault(u => u.Username == username);

            if (user == null) return null; // Không tìm thấy user

            // 2. Kiểm tra mật khẩu (So sánh pass nhập vào với hash trong DB)
            bool isPasswordCorrect = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

            if (isPasswordCorrect)
            {
                return user; // Đăng nhập thành công
            }

            return null; // Sai mật khẩu
        }

        // Hàm tiện ích: Tạo Admin mặc định nếu DB chưa có ai (Chạy 1 lần đầu)
        public void CreateDefaultAdmin()
        {
            if (!_context.Users.Any())
            {
                var admin = new User
                {
                    Username = "admin",
                    // Mã hóa mật khẩu "123456"
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                    FullName = "Quản Trị Viên",
                    RoleId = 1, // 1 = Admin
                    IsActive = true
                };

                _context.Users.Add(admin);
                _context.SaveChanges();
            }
        }
    }
}