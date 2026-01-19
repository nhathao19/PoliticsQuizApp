using Microsoft.Win32;
using PoliticsQuizApp.WPF.Services;
using System.Windows;
using System.Windows.Controls;

namespace PoliticsQuizApp.WPF
{
    public partial class UserManagementWindow : Window
    {
        private UserService _userService;

        public UserManagementWindow()
        {
            InitializeComponent();
            _userService = new UserService();
            LoadData();
        }

        private void LoadData()
        {
            // 1. Lấy danh sách gốc từ Service
            var rawUsers = _userService.GetAllUsers();

            // 2. Chuyển đổi sang danh sách hiển thị có STT
            var displayList = new System.Collections.Generic.List<UserDisplayVM>();
            int index = 1;

            foreach (var u in rawUsers)
            {
                // Giả sử u có các thuộc tính UserId, Username, FullName, RoleName
                // Chúng ta map sang class hiển thị mới
                displayList.Add(new UserDisplayVM
                {
                    STT = index++,
                    UserId = u.UserId,      // Giữ lại để dùng cho nút Xóa/Sửa
                    Username = u.Username,
                    FullName = u.FullName,
                    RoleName = u.RoleName
                });
            }

            // 3. Gán vào DataGrid
            dgUsers.ItemsSource = displayList;
        }

        private void BtnAddUser_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Kiểm tra xem đã chọn Role chưa
                if (cboRole.SelectedItem == null)
                {
                    MessageBox.Show("Chưa chọn vai trò!");
                    return;
                }

                string roleName = (cboRole.SelectedItem as ComboBoxItem).Content.ToString();
                byte roleId = 3; // Mặc định là Student

                // Xác định Role ID
                switch (roleName)
                {
                    case "Admin": roleId = 1; break;
                    case "Teacher": roleId = 2; break;
                    case "Student": roleId = 3; break;
                }

                // --- TRƯỜNG HỢP 1: THÊM SINH VIÊN (TỰ ĐỘNG SINH ACCESS CODE) ---
                if (roleId == 3)
                {
                    // Sinh viên thì KHÔNG cần nhập Mật khẩu (vì hệ thống tự tạo)
                    if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtFullName.Text))
                    {
                        MessageBox.Show("Vui lòng nhập Mã Sinh Viên và Họ Tên!");
                        return;
                    }

                    // Gọi hàm sinh mã (Đảm bảo bạn đã thêm hàm AddStudentWithCode vào UserService.cs ở bước trước)
                    string accessCode = _userService.AddStudentWithCode(txtUsername.Text, txtFullName.Text);

                    if (accessCode != null)
                    {
                        // Hiện thông báo chứa mã Access Code để Admin biết
                        MessageBox.Show($"Tạo Sinh viên thành công!\n\n" +
                                        $"- Mã SV: {txtUsername.Text}\n" +
                                        $"- Mã Truy Cập (Access Code): {accessCode}\n\n" +
                                        $"(Hãy COPY mã này gửi cho sinh viên để họ vào thi)",
                                        "Thông báo Mật khẩu", MessageBoxButton.OK, MessageBoxImage.Information);

                        LoadData();
                        ClearForm();
                    }
                    else
                    {
                        MessageBox.Show("Thất bại! Mã sinh viên này đã tồn tại.");
                    }
                }
                // --- TRƯỜNG HỢP 2: THÊM ADMIN / GIÁO VIÊN (NHẬP MẬT KHẨU TAY) ---
                else
                {
                    // Admin/Teacher bắt buộc phải nhập Password thủ công
                    if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
                    {
                        MessageBox.Show("Với Admin/Giáo viên, vui lòng nhập đủ Tên đăng nhập và Mật khẩu!");
                        return;
                    }

                    bool result = _userService.AddUser(txtUsername.Text, txtPassword.Text, txtFullName.Text, roleId);

                    if (result)
                    {
                        MessageBox.Show("Thêm quản trị viên/giáo viên thành công!");
                        LoadData();
                        ClearForm();
                    }
                    else
                    {
                        MessageBox.Show("Thất bại! Tên đăng nhập đã tồn tại.");
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Đã xảy ra lỗi hệ thống: " + ex.Message);
            }
        }

        private void BtnDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null && int.TryParse(btn.Tag.ToString(), out int userId))
            {
                var confirm = MessageBox.Show($"Bạn có chắc muốn xóa User ID {userId}?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (confirm == MessageBoxResult.Yes)
                {
                    if (_userService.DeleteUser(userId))
                    {
                        LoadData();
                        MessageBox.Show("Đã xóa thành công.");
                    }
                    else
                    {
                        MessageBox.Show("Không thể xóa (Có thể user này đã có dữ liệu thi).");
                    }
                }
            }
        }

        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Excel Files|*.xlsx;*.xls",
                Title = "Chọn danh sách sinh viên (Cột A: MSSV, Cột B: Tên)"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                // Gọi hàm Import
                string message = _userService.ImportStudentsFromExcel(openFileDialog.FileName);
                MessageBox.Show(message);
                LoadData(); // Load lại bảng
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                FileName = "DanhSach_MatKhau_SinhVien.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    _userService.ExportStudentList(saveFileDialog.FileName);
                    MessageBox.Show("Xuất file thành công! Bạn có thể mở file để xem mã Access Code.");
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("Lỗi khi xuất file: " + ex.Message);
                }
            }
        }

        // Hàm xóa trắng form cho gọn code
        private void ClearForm()
        {
            txtUsername.Clear();
            txtPassword.Clear();
            txtFullName.Clear();
            cboRole.SelectedIndex = 0;
        }
        public class UserDisplayVM
        {
            public int STT { get; set; }
            public int UserId { get; set; }
            public string Username { get; set; }
            public string FullName { get; set; }
            public string RoleName { get; set; }
        }
    }
}