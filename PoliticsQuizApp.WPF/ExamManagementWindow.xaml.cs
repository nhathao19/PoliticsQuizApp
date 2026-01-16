using PoliticsQuizApp.WPF.Services;
using System.Windows;
using System.Windows.Controls;

namespace PoliticsQuizApp.WPF
{
    public partial class ExamManagementWindow : Window
    {
        private ExamService _examService;

        public ExamManagementWindow()
        {
            InitializeComponent();
            _examService = new ExamService();
            LoadData();
        }

        private void LoadData()
        {
            // 1. Load Chủ đề
            cboTopics.ItemsSource = _examService.GetAllTopics();

            // 2. Load Đề thi
            _examService = new ExamService();
            dgExams.ItemsSource = _examService.GetAllExams();
        }
        private void BtnAddExam_Click(object sender, RoutedEventArgs e)
        {
            // Validate cơ bản
            if (string.IsNullOrWhiteSpace(txtExamCode.Text) || string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                MessageBox.Show("Vui lòng nhập Mã và Tiêu đề!");
                return;
            }

            int duration = int.Parse(txtDuration.Text);
            int easy = int.Parse(txtEasy.Text);
            int med = int.Parse(txtMedium.Text);
            int hard = int.Parse(txtHard.Text);

            if (easy + med + hard == 0)
            {
                MessageBox.Show("Phải có ít nhất 1 câu hỏi!");
                return;
            }

            int? topicId = null;
            if (cboTopics.SelectedValue != null)
                topicId = (int)cboTopics.SelectedValue;

            // Gọi hàm AddExam mới đã update
            bool result = _examService.AddExam(txtExamCode.Text, txtTitle.Text, topicId, duration, easy, med, hard);

            if (result)
            {
                MessageBox.Show("Tạo đề thành công!");
                LoadData();
                txtExamCode.Clear(); txtTitle.Clear();
            }
            else
            {
                MessageBox.Show("Lỗi! Mã đề trùng hoặc lỗi kết nối.");
            }
        }
        private void BtnToggleStatus_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && int.TryParse(btn.Tag.ToString(), out int id))
            {
                _examService.ToggleExamStatus(id);
                LoadData();
            }
        }
        private void BtnDeleteExam_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && int.TryParse(btn.Tag.ToString(), out int id))
            {
                if (MessageBox.Show("Xóa đề thi này?", "Xác nhận", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _examService.DeleteExam(id);
                    LoadData();
                }
            }
        }
        private void CalculateTotal_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Kiểm tra xem các ô đã được khởi tạo chưa (tránh lỗi null khi mới mở form)
            if (txtEasy == null || txtMedium == null || txtHard == null || lblTotalQuestions == null)
                return;

            // Dùng TryParse để nếu người dùng xóa trắng hoặc nhập chữ thì tính là 0
            int easy = 0, medium = 0, hard = 0;

            int.TryParse(txtEasy.Text, out easy);
            int.TryParse(txtMedium.Text, out medium);
            int.TryParse(txtHard.Text, out hard);

            int total = easy + medium + hard;

            // Cập nhật lên màn hình
            lblTotalQuestions.Text = total.ToString();
        }
    }
}