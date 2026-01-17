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

        // --- 2. CONSTRUCTOR
        public ExamWindow(Exam exam, string studentName, int studentId)
        {
            InitializeComponent();

            // 1. KHỞI TẠO SERVICE NGAY LẬP TỨC
            _examService = new ExamService();

            // 2. Validate dữ liệu đầu vào
            if (exam == null)
            {
                MessageBox.Show("Lỗi: Dữ liệu đề thi bị Null.");
                Close();
                return;
            }

            _currentExam = exam;
            _studentName = studentName;
            _studentId = studentId;
            _currentExamId = exam.ExamId; // Quan trọng: Lấy ID để truy vấn

            // 3. Hiển thị thông tin cơ bản
            if (lblExamTitle != null) lblExamTitle.Text = _currentExam.Title;
            if (lblStudentName != null) lblStudentName.Text = _studentName;

            // 4. LẤY CÂU HỎI TỪ BẢNG EXAMQUESTIONS (Đã sửa ở Bước 2)
            try
            {
                var rawQuestions = _examService.GetQuestionsByExamId(_currentExamId);

                if (rawQuestions == null || rawQuestions.Count == 0)
                {
                    MessageBox.Show($"Đề thi (ID: {_currentExamId}) chưa có câu hỏi nào trong hệ thống!\nVui lòng kiểm tra lại quá trình tạo đề.",
                                    "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Close();
                    return;
                }

                // Convert sang ViewModel (theo code cũ của bạn)
                _questions = rawQuestions.Select(q => new QuestionViewModel
                {
                    QuestionData = q,
                    IsSelectedInManager = false
                }).ToList();

                // 5. Bắt đầu thi (Logic cũ giữ nguyên)
                StartExam();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải câu hỏi: " + ex.Message);
                Close();
            }
        }

        // --- 3. LOGIC BẮT ĐẦU ---
        private void StartExam()
        {
            if (_currentExam == null) { MessageBox.Show("Lỗi dữ liệu đề thi!"); Close(); return; }

            _startTime = DateTime.Now;


            // Kiểm tra lại danh sách đã lấy từ Constructor
            if (_questions == null || _questions.Count == 0)
            {
                MessageBox.Show("Đề thi này chưa có câu hỏi nào! Vui lòng liên hệ Giám thị.");
                Close();
                return;
            }

            // Gán dữ liệu vào giao diện (Giữ nguyên các dòng tiếp theo của bạn)
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
                        SelectedAnswerIds = q.UserSelectedAnswerIds,
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
                                q.UserSelectedAnswerIds = savedAns.SelectedAnswerIds ?? new List<long>();
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

            // Hiện thông báo nhỏ nếu là câu nhiều đáp án
            if (qVM.IsMultipleChoice)
            {
                TextBlock hint = new TextBlock
                {
                    Text = "(Câu hỏi chọn nhiều đáp án)",
                    Foreground = System.Windows.Media.Brushes.Gray,
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                pnlAnswers.Children.Add(hint);
            }
            foreach (var ans in qVM.Answers)
            {
                // TẠO CONTROL TÙY THUỘC VÀO LOẠI CÂU HỎI
                Control answerControl;

                if (qVM.IsMultipleChoice)
                {
                    // 1. Dùng CHECKBOX (Hình vuông)
                    CheckBox cb = new CheckBox();
                    cb.Content = new TextBlock { Text = ans.Content, TextWrapping = TextWrapping.Wrap, FontSize = 16 };
                    cb.Margin = new Thickness(0, 5, 0, 10);
                    cb.Tag = ans.AnswerId;

                    // Sự kiện Click
                    cb.Checked += (s, e) => ToggleAnswer(qVM, (long)((CheckBox)s).Tag, true);
                    cb.Unchecked += (s, e) => ToggleAnswer(qVM, (long)((CheckBox)s).Tag, false);

                    // Load trạng thái cũ
                    if (qVM.UserSelectedAnswerIds.Contains(ans.AnswerId)) cb.IsChecked = true;

                    answerControl = cb;
                }
                else
                {
                    // 2. Dùng RADIOBUTTON (Hình tròn)
                    RadioButton rb = new RadioButton();
                    rb.Content = new TextBlock { Text = ans.Content, TextWrapping = TextWrapping.Wrap, FontSize = 16 };
                    rb.Margin = new Thickness(0, 5, 0, 10);
                    rb.Tag = ans.AnswerId;

                    // Sự kiện Click
                    rb.Checked += (s, e) => {
                        // Radio chỉ chọn 1 -> Xóa hết cái cũ, thêm cái mới
                        qVM.UserSelectedAnswerIds.Clear();
                        qVM.UserSelectedAnswerIds.Add((long)((RadioButton)s).Tag);
                        UpdateProgress();
                    };

                    // Load trạng thái cũ
                    if (qVM.UserSelectedAnswerIds.Contains(ans.AnswerId)) rb.IsChecked = true;

                    answerControl = rb;
                }

                pnlAnswers.Children.Add(answerControl);
            }
        }
        // Hàm phụ để thêm/bớt đáp án vào list
        private void ToggleAnswer(QuestionViewModel qVM, long ansId, bool isChecked)
        {
            if (isChecked)
            {
                if (!qVM.UserSelectedAnswerIds.Contains(ansId))
                    qVM.UserSelectedAnswerIds.Add(ansId);
            }
            else
            {
                if (qVM.UserSelectedAnswerIds.Contains(ansId))
                    qVM.UserSelectedAnswerIds.Remove(ansId);
            }

            // Cập nhật giao diện (cần gọi PropertyChanged cho UserSelectedAnswerIds nhưng List ko tự báo)
            // Ta gọi UpdateProgress để cập nhật thanh tiến độ
            UpdateProgress();

            // Ép cập nhật màu sắc Navigator
            qVM.UserSelectedAnswerIds = new List<long>(qVM.UserSelectedAnswerIds);
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
            // Dừng đồng hồ
            if (_timer != null) _timer.Stop();

            // 1. TÍNH ĐIỂM (LOGIC MỚI CHO MULTIPLE CHOICE)
            double score = 0;
            int correctCount = 0;
            
            // Tránh chia cho 0 nếu đề không có câu nào
            if (_questions.Count > 0)
            {
                double scorePerQuestion = 10.0 / _questions.Count;

                foreach (var q in _questions)
                {
                    // Lấy danh sách ID các đáp án ĐÚNG trong Database
                    var correctIds = q.Answers.Where(a => a.IsCorrect)
                                              .Select(a => a.AnswerId)
                                              .ToList();

                    // Lấy danh sách ID sinh viên ĐÃ CHỌN
                    // (Đây là chỗ gây lỗi cũ nếu dùng q.UserSelectedAnswerId)
                    var userIds = q.UserSelectedAnswerIds; 

                    // SO SÁNH:
                    // Sinh viên phải chọn ĐỦ số lượng và ĐÚNG các ID
                    if (correctIds.Count == userIds.Count && !correctIds.Except(userIds).Any())
                    {
                        correctCount++;
                        score += scorePerQuestion;
                    }
                }
            }

            // Làm tròn điểm số (2 chữ số thập phân)
            score = Math.Round(score, 2);

            // 2. GỬI KẾT QUẢ XUỐNG DATABASE
            bool success = _examService.SubmitExam(_studentId, _currentExam.ExamId, score, _startTime, _questions);

            if (success)
            {
                // Xóa file lưu tạm (nếu có)
                if (System.IO.File.Exists(_tempFilePath))
                {
                    try { System.IO.File.Delete(_tempFilePath); } catch { }
                }

                MessageBox.Show($"Nộp bài thành công!\n\nSố câu đúng: {correctCount}/{_questions.Count}\nĐiểm số: {score}", 
                                "Kết quả", MessageBoxButton.OK, MessageBoxImage.Information);
                
                this.Close(); // Đóng cửa sổ thi
            }
            else
            {
                MessageBox.Show("Lỗi kết nối CSDL! Kết quả chưa được lưu.\nVui lòng báo giám thị ngay lập tức.", 
                                "Lỗi Nghiêm Trọng", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
            public List<long> SelectedAnswerIds { get; set; }
            public bool IsFlagged { get; set; }
        }
    }
}