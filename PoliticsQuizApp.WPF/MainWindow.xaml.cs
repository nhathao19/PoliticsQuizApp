using PoliticsQuizApp.WPF.Services;
using System.Windows;
using System.Windows.Controls;

namespace PoliticsQuizApp.WPF
{
    public partial class MainWindow : Window
    {
        private readonly QuestionService _questionService;
        public MainWindow()
        {
            InitializeComponent();
            _questionService = new QuestionService();

            // Tải dữ liệu ngay khi mở cửa sổ
            LoadQuestions();
        }
        private void LoadQuestions()
        {
            // Tạo mới service mỗi lần load để đảm bảo kết nối mới hoàn toàn
            QuestionService qs = new QuestionService();
            // Gán dữ liệu vào bảng
            dgQuestions.ItemsSource = qs.GetAllQuestions();
        }
        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadQuestions();
        }
        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow login = new LoginWindow();
            login.Show();
            this.Close();
        }
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            // Mở cửa sổ thêm mới dạng Dialog (người dùng phải đóng nó mới quay lại được màn hình chính)
            AddQuestionWindow addWindow = new AddQuestionWindow();

            // Nếu thêm thành công (DialogResult == true) thì tải lại danh sách
            if (addWindow.ShowDialog() == true)
            {
                LoadQuestions();
            }
        }
        private void BtnReport_Click(object sender, RoutedEventArgs e)
        {
            ReportWindow report = new ReportWindow();
            report.ShowDialog();
        }
        private void BtnImportUI_Click(object sender, RoutedEventArgs e)
        {
            ImportWindow importWin = new ImportWindow();
            importWin.ShowDialog();
            LoadQuestions(); // Tải lại danh sách sau khi import xong
        }
        private void BtnUserMgmt_Click(object sender, RoutedEventArgs e)
        {
            UserManagementWindow userWin = new UserManagementWindow();
            userWin.ShowDialog();
        }
        private void BtnDeleteQuestion_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && long.TryParse(btn.Tag.ToString(), out long qId))
            {
                var result = MessageBox.Show($"Bạn chắc chắn muốn xóa câu hỏi ID {qId}?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    // Gọi Service xóa
                    QuestionService qs = new QuestionService();
                    if (qs.DeleteQuestion(qId))
                    {
                        MessageBox.Show("Đã xóa thành công!");
                        LoadQuestions(); // Load lại bảng để mất dòng vừa xóa
                    }
                    else
                    {
                        MessageBox.Show("Xóa thất bại (Có thể do lỗi mạng hoặc CSDL).");
                    }
                }
            }
        }
        private void BtnEditQuestion_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && long.TryParse(btn.Tag.ToString(), out long qId))
            {
                // Chỉ cần truyền ID
                EditQuestionWindow editWin = new EditQuestionWindow(qId);

                if (editWin.ShowDialog() == true)
                {
                    LoadQuestions(); // Load lại bảng sau khi sửa xong
                }
            }
        }
        private void BtnExamManager_Click(object sender, RoutedEventArgs e)
        {
            ExamManagementWindow examWin = new ExamManagementWindow();
            examWin.ShowDialog();
        }
    }
}