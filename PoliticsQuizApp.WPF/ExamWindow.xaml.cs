using PoliticsQuizApp.Data.Models;
using PoliticsQuizApp.WPF.Services;
using PoliticsQuizApp.WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace PoliticsQuizApp.WPF
{
    public partial class ExamWindow : Window
    {
        // --- 1. KHAI BÁO BIẾN TOÀN CỤC (FIELDS) ---
        private ExamService _examService;
        private List<QuestionViewModel> _questions;
        private int _currentIndex = 0;
        private DispatcherTimer _timer;
        private TimeSpan _timeRemaining;
        private DateTime _startTime;

        // Các biến dữ liệu quan trọng
        private Exam _currentExam;
        private string _studentName;
        private int _studentId;         // <--- Biến lưu ID người thi
        private int _currentExamId;     // <--- Biến lưu ID đề thi
        private int _violationCount = 0; // Đếm số lần vi phạm
        private const int MAX_VIOLATIONS = 3; // Giới hạn số lần cho phép (ví dụ 3 lần)

        // Đường dẫn file tạm (Auto Save)
        private string _tempFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PoliticsQuiz_Temp.json");

        // --- 2. CONSTRUCTOR CHUẨN (DUY NHẤT) ---
        public ExamWindow(Exam exam, string studentName, int studentId)
        {
            InitializeComponent();

            _currentExam = exam;
            _studentName = studentName;
            _studentId = studentId;       // Lưu ID sinh viên vào biến toàn cục
            _currentExamId = exam.ExamId; // Lưu ID đề thi

            _examService = new ExamService();

            // Hiển thị thông tin lên giao diện
            if (FindName("lblExamTitle") is TextBlock lblTitle) lblTitle.Text = _currentExam.Title;
            if (FindName("lblStudentName") is TextBlock lblName) lblName.Text = $"Thí sinh: {_studentName}";

            StartExam();
            // Đăng ký sự kiện: Khi cửa sổ mất tiêu điểm (Người dùng Alt+Tab ra ngoài)
            this.Deactivated += ExamWindow_Deactivated;
            // Đăng ký sự kiện: Chặn các phím tắt đơn giản (Esc)
            this.KeyDown += ExamWindow_KeyDown;
        }

        // --- 3. LOGIC BẮT ĐẦU ---
        private void StartExam()
        {
            if (_currentExam == null) { MessageBox.Show("Lỗi dữ liệu đề thi!"); Close(); return; }

            _startTime = DateTime.Now;

            // Lấy câu hỏi từ Service
            _questions = _examService.GenerateExamQuestions(_currentExamId, 0); // Tham số thứ 2 là limit, service mới tự tính nên để 0 hoặc bỏ nếu đã sửa service

            if (_questions == null || _questions.Count == 0)
            {
                MessageBox.Show("Đề thi này chưa có câu hỏi nào! Vui lòng liên hệ Giám thị.");
                Close();
                return;
            }

            // Gán dữ liệu vào giao diện
            icNavigator.ItemsSource = null;
            icNavigator.Items.Clear();
            icNavigator.ItemsSource = _questions;
            UpdateProgress();

            // Cài đặt Timer
            _timeRemaining = TimeSpan.FromMinutes(_currentExam.DurationMinutes);
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();

            // Hiển thị câu đầu tiên
            DisplayQuestion(0);

            // Phục hồi bài cũ nếu có sự cố
            TryRecoverExam();
        }

        // --- 4. ĐỒNG HỒ & AUTO SAVE ---
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_timeRemaining.TotalSeconds > 0)
            {
                _timeRemaining = _timeRemaining.Subtract(TimeSpan.FromSeconds(1));
                lblTimer.Text = _timeRemaining.ToString(@"mm\:ss");

                // Cảnh báo khi còn dưới 5 phút
                if (_timeRemaining.TotalMinutes < 5) lblTimer.Foreground = System.Windows.Media.Brushes.Red;

                // Auto Save mỗi 30 giây (giây thứ 0 và 30)
                if (_timeRemaining.Seconds % 30 == 0) AutoSaveProgress();
            }
            else
            {
                _timer.Stop();
                MessageBox.Show("Hết giờ làm bài!", "Thông báo");
                FinishExam();
            }
        }

        private void AutoSaveProgress()
        {
            if (_questions == null) return;

            try
            {
                var state = new ExamTempState
                {
                    ExamId = _currentExamId,
                    TimeRemainingSeconds = _timeRemaining.TotalSeconds,
                    Answers = _questions.Select(q => new StudentAnswerTemp
                    {
                        QuestionId = q.QuestionData.QuestionID,
                        SelectedAnswerId = q.UserSelectedAnswerId,
                        IsFlagged = q.IsFlagged
                    }).ToList()
                };

                string json = JsonSerializer.Serialize(state);
                File.WriteAllText(_tempFilePath, json);
            }
            catch { /* Bỏ qua lỗi ghi file */ }
        }

        private void TryRecoverExam()
        {
            if (!File.Exists(_tempFilePath)) return;

            try
            {
                string json = File.ReadAllText(_tempFilePath);
                var state = JsonSerializer.Deserialize<ExamTempState>(json);

                // Chỉ phục hồi nếu đúng là bài thi này
                if (state != null && state.ExamId == _currentExamId)
                {
                    var result = MessageBox.Show("Hệ thống phát hiện bài làm trước đó bị gián đoạn. Bạn có muốn phục hồi không?",
                                                 "Phục hồi bài thi", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        _timeRemaining = TimeSpan.FromSeconds(state.TimeRemainingSeconds);
                        foreach (var savedAns in state.Answers)
                        {
                            var q = _questions.FirstOrDefault(x => x.QuestionData.QuestionID == savedAns.QuestionId);
                            if (q != null)
                            {
                                q.UserSelectedAnswerId = savedAns.SelectedAnswerId;
                                q.IsFlagged = savedAns.IsFlagged;
                            }
                        }
                        UpdateProgress();
                        DisplayQuestion(_currentIndex);
                    }
                }
            }
            catch { /* File lỗi thì bỏ qua */ }
        }

        // --- 5. HIỂN THỊ CÂU HỎI ---
        private void DisplayQuestion(int index)
        {
            if (_questions == null || index < 0 || index >= _questions.Count) return;

            if (_questions[_currentIndex] != null)
                _questions[_currentIndex].IsSelected = false;

            _currentIndex = index;
            var qVM = _questions[index];
            qVM.IsSelected = true;

            // Binding dữ liệu
            gridQuestionContent.DataContext = qVM;
            if (FindName("chkFlag") is CheckBox chk) chk.DataContext = qVM;

            // Vẽ đáp án
            pnlAnswers.Children.Clear();
            foreach (var ans in qVM.Answers)
            {
                RadioButton rb = new RadioButton();
                if (Application.Current.Resources.Contains("AnswerCardStyle"))
                    rb.Style = (Style)Application.Current.Resources["AnswerCardStyle"];

                rb.Content = new TextBlock { Text = ans.Content, TextWrapping = TextWrapping.Wrap, FontSize = 16 };
                rb.Margin = new Thickness(0, 5, 0, 10);
                rb.Tag = ans.AnswerId;

                // Sự kiện chọn đáp án
                rb.Checked += (s, e) =>
                {
                    if (rb.IsChecked == true)
                    {
                        qVM.UserSelectedAnswerId = (long)((RadioButton)s).Tag;
                        UpdateProgress();
                    }
                };

                if (qVM.UserSelectedAnswerId == ans.AnswerId) rb.IsChecked = true;
                pnlAnswers.Children.Add(rb);
            }
        }

        private void UpdateProgress()
        {
            if (_questions == null) return;
            int count = _questions.Count(q => q.IsAnswered);
            lblProgress.Text = $"{count}/{_questions.Count}";
            prgBar.Maximum = _questions.Count;
            prgBar.Value = count;
        }

        // --- 6. SỰ KIỆN NÚT BẤM ---
        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex < _questions.Count - 1) DisplayQuestion(_currentIndex + 1);
        }

        private void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex > 0) DisplayQuestion(_currentIndex - 1);
        }

        private void BtnQuestionNav_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is QuestionViewModel qVM)
                DisplayQuestion(qVM.Index - 1);
        }

        private void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Bạn có chắc chắn muốn nộp bài?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                FinishExam();
            }
        }

        // --- 7. KẾT THÚC BÀI THI ---
        private bool _isSubmitted = false;

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_isSubmitted)
            {
                e.Cancel = true;
                MessageBox.Show("Bạn không thể thoát khi chưa nộp bài!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void FinishExam()
        {
            if (_timer != null) _timer.Stop();
            _isSubmitted = true;

            // Tính điểm
            int correctCount = 0;
            foreach (var q in _questions)
            {
                var selected = q.Answers.FirstOrDefault(a => a.AnswerId == q.UserSelectedAnswerId);
                if (selected != null && selected.IsCorrect) correctCount++;
            }

            double finalScore = (double)correctCount / _questions.Count * 10;

            // QUAN TRỌNG: Lưu kết quả với ID thật (_studentId) thay vì hardcode số 1
            bool isSaved = _examService.SubmitExam(_studentId, _currentExamId, finalScore, _startTime, _questions);

            // Vô hiệu hóa giao diện
            gridQuestionContent.IsEnabled = false;
            icNavigator.IsEnabled = false;

            // Xóa file tạm
            if (File.Exists(_tempFilePath)) File.Delete(_tempFilePath);

            if (isSaved)
            {
                MessageBox.Show($"Kết quả: {finalScore:F2} điểm\nSố câu đúng: {correctCount}/{_questions.Count}", "Hoàn thành");
            }
            else
            {
                MessageBox.Show("Lỗi khi lưu kết quả vào CSDL!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            this.Close();
        }


        // --- 8. XỬ LÝ VI PHẠM (ALT+TAB RA NGOÀI) --- 
        // 1. Xử lý khi sinh viên Alt+Tab ra ngoài
        private void ExamWindow_Deactivated(object sender, EventArgs e)
        {
            // Nếu bài thi đã nộp rồi thì không cần bắt lỗi nữa
            if (_isSubmitted) return;

            _violationCount++; // Tăng số lần vi phạm

            // Đưa cửa sổ lên trên cùng ngay lập tức
            this.Topmost = true;
            this.Activate();

            if (_violationCount < MAX_VIOLATIONS)
            {
                MessageBox.Show($"CẢNH BÁO: Bạn vừa cố gắng rời khỏi màn hình thi!\n" +
                                $"Nếu vi phạm {_violationCount}/{MAX_VIOLATIONS} lần, bài thi sẽ tự động nộp.",
                                "Cảnh báo gian lận", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                // Vi phạm quá số lần -> Nộp bài luôn
                MessageBox.Show("Bạn đã vi phạm quy chế thi quá số lần quy định.\nHệ thống sẽ tự động nộp bài ngay bây giờ!",
                                "ĐÌNH CHỈ THI", MessageBoxButton.OK, MessageBoxImage.Error);

                FinishExam(); // Gọi hàm nộp bài cưỡng chế
            }
        }
        // 2. Chặn phím ESC (để không cho thoát màn hình Fullscreen)
        private void ExamWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Nếu bấm ESC
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                e.Handled = true; // Hủy phím bấm đó
                MessageBox.Show("Không được phép sử dụng phím ESC trong khi thi!", "Cảnh báo");
            }
        }

        // --- CÁC CLASS PHỤ TRỢ (Để JSON Serialize hoạt động) ---
        public class ExamTempState
        {
            public int ExamId { get; set; }
            public double TimeRemainingSeconds { get; set; }
            public List<StudentAnswerTemp> Answers { get; set; }
        }

        public class StudentAnswerTemp
        {
            public long QuestionId { get; set; }
            public long? SelectedAnswerId { get; set; }
            public bool IsFlagged { get; set; }
        }
    }
}