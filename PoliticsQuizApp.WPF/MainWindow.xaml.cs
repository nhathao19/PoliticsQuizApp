using PoliticsQuizApp.WPF.Services;
using PoliticsQuizApp.WPF.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace PoliticsQuizApp.WPF
{
    public partial class MainWindow : Window
    {
        private QuestionService _service;

        public MainWindow()
        {
            InitializeComponent();
            _service = new QuestionService();
            LoadQuestions();
        }

        private void LoadQuestions()
        {
            // Tải danh sách câu hỏi cho bảng tra cứu bên dưới
            var list = _service.GetAllQuestions();
            dgQuestions.ItemsSource = list;
        }

        // =========================================================
        // 1. NHÓM QUẢN LÝ ĐỀ THI & CÂU HỎI (Hàng Trên)
        // =========================================================

        // [Nút 1] Mở Quản Lý Theo Chương
        private void BtnOpenManager_Click(object sender, RoutedEventArgs e)
        {
            // Gọi cửa sổ QuestionManagerWindow đã có
            QuestionManagerWindow win = new QuestionManagerWindow();
            win.ShowDialog();
            LoadQuestions(); // Refresh sau khi đóng
        }

        // [Nút 2] Cấu Hình Đề Thi (Danh sách đề thi)
        private void BtnExamConfig_Click(object sender, RoutedEventArgs e)
        {
            // Gọi cửa sổ ExamManagementWindow đã có
            ExamManagementWindow win = new ExamManagementWindow();
            win.ShowDialog();
        }

        // [Nút 3] Tạo Đề Ma Trận (Tự động)
        private void BtnCreateExam_Click(object sender, RoutedEventArgs e)
        {
            // Gọi cửa sổ GenerateExamWindow đã có
            GenerateExamWindow win = new GenerateExamWindow();
            win.ShowDialog();
        }

        // =========================================================
        // 2. NHÓM TIỆN ÍCH (Hàng Giữa)
        // =========================================================

        // [Nút 4] Import Excel
        private void BtnImportExcel_Click(object sender, RoutedEventArgs e)
        {
            // Gọi cửa sổ ImportWindow đã có
            ImportWindow win = new ImportWindow();
            win.ShowDialog();
            LoadQuestions(); // Refresh sau khi import
        }

        // [Nút 5] Xem Kết Quả
        private void BtnReport_Click(object sender, RoutedEventArgs e)
        {
            // Gọi cửa sổ ReportWindow đã có
            ReportWindow win = new ReportWindow();
            win.ShowDialog();
        }

        // [Nút 6] Quản Lý Người Dùng
        private void BtnUserMgmt_Click(object sender, RoutedEventArgs e)
        {
            // Gọi cửa sổ UserManagementWindow đã có
            UserManagementWindow win = new UserManagementWindow();
            win.ShowDialog();
        }

        // =========================================================
        // 3. NHÓM TRA CỨU NHANH (Bảng Dưới Cùng)
        // =========================================================

        private void BtnAddQuestion_Click(object sender, RoutedEventArgs e)
        {
            AddQuestionWindow win = new AddQuestionWindow();
            if (win.ShowDialog() == true) LoadQuestions();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadQuestions();
        }

        // Sửa câu hỏi (Nút nhỏ trong bảng)
        private void BtnEditQuestion_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is long id)
            {
                EditQuestionWindow win = new EditQuestionWindow(id);
                if (win.ShowDialog() == true) LoadQuestions();
            }
        }

        // Xóa câu hỏi (Nút nhỏ trong bảng)
        private void BtnDeleteQuestion_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is long id)
            {
                if (MessageBox.Show("Bạn có chắc chắn muốn xóa câu hỏi này?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    _service.DeleteQuestion(id);
                    LoadQuestions();
                }
            }
        }

        // Đăng xuất
        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow login = new LoginWindow();
            login.Show();
            this.Close();
        }
    }
}