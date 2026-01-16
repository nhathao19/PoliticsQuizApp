using PoliticsQuizApp.WPF.Services;
using System.Windows;

namespace PoliticsQuizApp.WPF
{
    public partial class LoginWindow : Window
    {
        private AuthService _authService;

        public LoginWindow()
        {
            InitializeComponent();
            _authService = new AuthService();

            // Tự động tạo admin/123456 nếu chưa có
            _authService.CreateDefaultAdmin();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text;
            string password = txtPassword.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                lblStatus.Text = "Vui lòng nhập đầy đủ thông tin!";
                return;
            }

            // Gọi Service kiểm tra
            var user = _authService.Login(username, password);

            if (user != null)
            {
                if (user.IsActive == true)
                {
                    // Mở Dashboard
                    MainWindow dashboard = new MainWindow();
                    dashboard.Show();

                    this.Close(); // Đóng cửa sổ login
                }
                else
                {
                    lblStatus.Text = "Tài khoản này đã bị khóa.";
                }
            }
            else
            {
                lblStatus.Text = "Sai tên đăng nhập hoặc mật khẩu!";
            }
        }
    }
}