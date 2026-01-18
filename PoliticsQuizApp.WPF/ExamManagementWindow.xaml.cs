using PoliticsQuizApp.Data;
using PoliticsQuizApp.Data.Models;
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
            // 1. Kiểm tra đầu vào
            if (string.IsNullOrWhiteSpace(txtExamCode.Text) || string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                MessageBox.Show("Vui lòng nhập Mã và Tiêu đề!");
                return;
            }

            int duration = int.Parse(txtDuration.Text);
            int easy = int.Parse(txtEasy.Text);
            int med = int.Parse(txtMedium.Text);
            int hard = int.Parse(txtHard.Text);
            int topicId = (int)cboTopics.SelectedValue;
            int totalRequest = easy + med + hard;

            using (var context = new PoliticsQuizDbContext())
            {
                using (var transaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        // A. Lưu thông tin đề thi
                        var exam = new Exam
                        {
                            ExamCode = txtExamCode.Text,
                            Title = txtTitle.Text,
                            DurationMinutes = duration,
                            TotalQuestions = totalRequest,
                            IsActive = true,

                            // --- ĐOẠN CẦN SỬA/THÊM ---
                            // Phải gán giá trị cho các cột này để hiển thị đúng 0/0/0 và để hàm lấy đề chạy đúng
                            EasyCount = easy,
                            MediumCount = med,
                            HardCount = hard,
                            TopicID = topicId, // Nên lưu cả TopicID nếu đề này chỉ dành cho 1 chủ đề
                                               // -------------------------

                            ConfigMatrix = $"Dễ: {easy} | TB: {med} | Khó: {hard}"
                        };
                        context.Exams.Add(exam);
                        context.SaveChanges();
                        // B. Nhặt câu hỏi và LƯU VÀO BẢNG TRUNG GIAN
                        var questions = new List<Question>();

                        // Lấy câu dễ
                        if (easy > 0)
                            questions.AddRange(context.Questions
                                .Where(q => q.TopicId == topicId && q.Difficulty == 1)
                                .OrderBy(x => Guid.NewGuid())
                                .Take(easy));

                        // Lấy câu trung bình
                        if (med > 0)
                            questions.AddRange(context.Questions
                                .Where(q => q.TopicId == topicId && q.Difficulty == 2)
                                .OrderBy(x => Guid.NewGuid())
                                .Take(med));

                        // Lấy câu khó
                        if (hard > 0)
                            questions.AddRange(context.Questions
                                .Where(q => q.TopicId == topicId && q.Difficulty == 3)
                                .OrderBy(x => Guid.NewGuid())
                                .Take(hard));

                        // Lưu vào bảng ExamQuestions (Để chốt cứng đề thi này)
                        foreach (var q in questions)
                        {
                            context.ExamQuestions.Add(new ExamQuestion
                            {
                                ExamId = exam.ExamId,
                                QuestionId = q.QuestionID
                            });
                        }

                        context.SaveChanges();
                        transaction.Commit();
                        MessageBox.Show("Tạo đề thành công!");
                        LoadData();
                    }
                    catch (Exception ex) { transaction.Rollback(); MessageBox.Show("Lỗi: " + ex.Message); }
                }
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