using OfficeOpenXml;
using PoliticsQuizApp.Data;
using PoliticsQuizApp.Data.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PoliticsQuizApp.WPF.Services
{
    // Class phụ để hiển thị lên bảng (DTO)
    public class UserDisplayModel
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string RoleName { get; set; } // Cột này để hiện chữ
    }

    public class UserService
    {
        private readonly PoliticsQuizDbContext _context;

        public UserService()
        {
            _context = new PoliticsQuizDbContext();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        // Lấy danh sách hiển thị (Đã đổi số RoleID thành chữ)
        public List<UserDisplayModel> GetAllUsers()
        {
            // Kết nối vào bảng Users
            return _context.Users.Select(u => new UserDisplayModel
            {
                UserId = u.UserId,
                Username = u.Username,
                FullName = u.FullName,
                RoleName = GetRoleName(u.RoleId) // Gọi hàm đổi số ra chữ
            }).OrderByDescending(u => u.UserId).ToList();
        }

        // Hàm phụ trợ đổi ID thành Tên
        private static string GetRoleName(int roleId)
        {
            return roleId switch
            {
                1 => "Admin",
                2 => "Giáo viên", // Teacher
                3 => "Sinh viên", // Student
                _ => "Khác"
            };
        }

        // Thêm người dùng
        public bool AddUser(string username, string password, string fullName, byte roleId)
        {
            try
            {
                if (_context.Users.Any(u => u.Username == username))
                    return false;

                var user = new User
                {
                    Username = username,
                    PasswordHash = password, // MD5 nếu cần
                    FullName = fullName,
                    RoleId = roleId,
                    IsActive = true
                };

                _context.Users.Add(user);
                _context.SaveChanges();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool DeleteUser(int userId)
        {
            try
            {
                var user = _context.Users.Find(userId);
                if (user == null) return false;
                _context.Users.Remove(user);
                _context.SaveChanges();
                return true;
            }
            catch { return false; }
        }

        private string GenerateAccessCode(int length = 6)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        public string AddStudentWithCode(string studentId, string fullName)
        {
            try
            {
                // Kiểm tra trùng mã sinh viên
                if (_context.Users.Any(u => u.Username == studentId))
                    return null; // Trùng ID

                // Sinh mã ngẫu nhiên
                string accessCode = GenerateAccessCode();

                var user = new User
                {
                    Username = studentId,   // Mã sinh viên (VD: SV001)
                    PasswordHash = accessCode,  // Mã truy cập (VD: X9D2A1) - Lưu thẳng hoặc mã hóa tùy bạn (ở đây lưu thô cho dễ quản lý)
                    FullName = fullName,
                    RoleId = 3              // Role Student
                };

                _context.Users.Add(user);
                _context.SaveChanges();

                return accessCode; // Trả về mã này để Admin copy gửi cho sinh viên
            }
            catch
            {
                return null;
            }
        }
        public User LoginStudent(string studentId, string accessCode)
        {
            // Tìm user có Username = StudentID và Password = AccessCode
            return _context.Users.FirstOrDefault(u =>
                u.Username == studentId &&
                u.PasswordHash == accessCode &&
                u.RoleId == 3);
        }
        public string ImportStudentsFromExcel(string filePath)
        {
            try
            {
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    int rowCount = worksheet.Dimension.Rows;
                    int successCount = 0;

                    for (int row = 2; row <= rowCount; row++) // Bỏ qua dòng tiêu đề
                    {
                        var mssv = worksheet.Cells[row, 1].Text.Trim(); // Cột 1: MSSV
                        var name = worksheet.Cells[row, 2].Text.Trim(); // Cột 2: Tên

                        if (string.IsNullOrEmpty(mssv) || string.IsNullOrEmpty(name)) continue;

                        // Kiểm tra trùng
                        if (!_context.Users.Any(u => u.Username == mssv))
                        {
                            var user = new User
                            {
                                Username = mssv,
                                FullName = name,
                                RoleId = 3, // Student
                                PasswordHash = GenerateAccessCode() // Tự sinh mã ngẫu nhiên
                            };
                            _context.Users.Add(user);
                            successCount++;
                        }
                    }
                    _context.SaveChanges();
                    return $"Đã thêm thành công {successCount} sinh viên!";
                }
            }
            catch (Exception ex)
            {
                return "Lỗi: " + ex.Message;
            }
        }
        public void ExportStudentList(string filePath)
        {
            var students = _context.Users.Where(u => u.RoleId == 3).ToList();

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var ws = package.Workbook.Worksheets.Add("DanhSachMa");

                // Tạo tiêu đề
                ws.Cells[1, 1].Value = "Mã Sinh Viên";
                ws.Cells[1, 2].Value = "Họ và Tên";
                ws.Cells[1, 3].Value = "ACCESS CODE (Mật khẩu)"; // Cột quan trọng nhất

                // Đổ dữ liệu
                for (int i = 0; i < students.Count; i++)
                {
                    ws.Cells[i + 2, 1].Value = students[i].Username;
                    ws.Cells[i + 2, 2].Value = students[i].FullName;
                    ws.Cells[i + 2, 3].Value = students[i].PasswordHash; // Lấy mã ra
                }

                ws.Columns.AutoFit();
                package.Save();
            }
        }
    }
}