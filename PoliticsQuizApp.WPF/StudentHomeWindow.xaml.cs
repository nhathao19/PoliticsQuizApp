using PoliticsQuizApp.Data.Models; // Cần dòng này để dùng biến 'User'
using PoliticsQuizApp.WPF.Services;
using System.Windows;

namespace PoliticsQuizApp.WPF
{
    public partial class StudentHomeWindow : Window
    {
        private ExamService _examService;
        private UserService _userService; // Thêm Service xử lý đăng nhập

        public StudentHomeWindow()
        {
            InitializeComponent();
            _examService = new ExamService();
            _userService = new UserService(); // Khởi tạo Service
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            // 1. Validate nhập liệu (Kiểm tra 3 ô thay vì 2 ô như trước)
            if (string.IsNullOrWhiteSpace(txtStudentId.Text) ||
                string.IsNullOrWhiteSpace(txtAccessCode.Text) ||
                string.IsNullOrWhiteSpace(txtExamCode.Text))
            {
                MessageBox.Show("Vui lòng nhập đủ: MSSV, Mã Truy Cập và Mã Đề Thi!", "Thiếu thông tin");
                return;
            }

            string studentId = txtStudentId.Text.Trim();
            string accessCode = txtAccessCode.Text.Trim();
            string examCode = txtExamCode.Text.Trim();

            // 2. BƯỚC MỚI: Đăng nhập để xác thực sinh viên
            // Gọi hàm LoginStudent mà ta vừa viết trong UserService
            User student = _userService.LoginStudent(studentId, accessCode);

            if (student == null)
            {
                MessageBox.Show("Sai Mã Sinh Viên hoặc Mã Truy Cập!\nVui lòng kiểm tra lại tờ giấy Admin cấp.",
                                "Đăng nhập thất bại", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 3. Kiểm tra Mã đề thi (Logic cũ)
            var exam = _examService.GetExamByCode(examCode);

            if (exam == null)
            {
                MessageBox.Show("Mã đề thi không tồn tại!", "Lỗi");
                return;
            }

            if (exam.IsActive != true)
            {
                MessageBox.Show("Đề thi này chưa mở hoặc đã kết thúc.", "Thông báo");
                return;
            }

            // 4. Vào thi - QUAN TRỌNG:
            // Truyền ID THẬT (student.UserId) lấy từ Database sang màn hình thi.
            // Lúc này bài thi sẽ được lưu chính chủ, không còn bị lỗi "Admin thi hộ" nữa.

            ExamWindow examWindow = new ExamWindow(exam, student.FullName, student.UserId);

            this.Hide(); // Ẩn màn hình chờ
            examWindow.ShowDialog();
            this.Show(); // Hiện lại khi thi xong

            // Xóa mã truy cập để người sau không nhìn thấy
            txtAccessCode.Clear();
        }

        private void BtnAdmin_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginForm = new LoginWindow();
            loginForm.Show();

            this.Close(); 
        }
    }
}