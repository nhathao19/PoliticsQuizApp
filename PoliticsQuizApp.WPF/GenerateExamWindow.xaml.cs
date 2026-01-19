using PoliticsQuizApp.Data;
using PoliticsQuizApp.Data.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace PoliticsQuizApp.WPF
{
    public partial class GenerateExamWindow : Window
    {
        private ObservableCollection<TopicSelectionViewModel> _topics;

        public GenerateExamWindow()
        {
            InitializeComponent();
            LoadMatrixData();
        }

        private void LoadMatrixData()
        {
            using (var context = new PoliticsQuizDbContext())
            {
                var data = context.Topics.Select(t => new TopicSelectionViewModel
                {
                    TopicId = t.TopicId,
                    TopicName = t.TopicName,
                    TotalAvailable = context.Questions.Count(q => q.TopicId == t.TopicId),
                    CountToSelect = 0
                }).ToList();

                _topics = new ObservableCollection<TopicSelectionViewModel>(data);
                foreach (var item in _topics) item.PropertyChanged += (s, e) => UpdateTotal();
                dgTopics.ItemsSource = _topics;
            }
        }

        private void UpdateTotal()
        {
            txtTotalSelected.Text = "Tổng số câu: " + _topics.Sum(x => x.CountToSelect);
        }

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            int total = _topics.Sum(x => x.CountToSelect);
            if (total == 0) { MessageBox.Show("Chưa chọn câu hỏi!"); return; }

            using (var context = new PoliticsQuizDbContext())
            {
                using (var transaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        // Bước 1: Random câu hỏi trước để biết chính xác số lượng Dễ/TB/Khó
                        var allSelectedQuestions = new List<Question>(); // List chứa toàn bộ câu hỏi được chọn

                        foreach (var t in _topics)
                        {
                            if (t.CountToSelect > 0)
                            {
                                var randomQ = context.Questions
                                                        .Where(q => q.TopicId == t.TopicId)
                                                        .OrderBy(x => Guid.NewGuid())
                                                        .Take(t.CountToSelect)
                                                        .ToList();
                                allSelectedQuestions.AddRange(randomQ);
                            }
                        }

                        // Bước 2: Tính toán thống kê từ danh sách đã chọn
                        int easyCount = allSelectedQuestions.Count(q => q.Difficulty == 1);
                        int mediumCount = allSelectedQuestions.Count(q => q.Difficulty == 2);
                        int hardCount = allSelectedQuestions.Count(q => q.Difficulty == 3);

                        // Bước 3: Tạo đối tượng Exam và gán thống kê
                        var exam = new Exam
                        {
                            ExamCode = txtExamCode.Text,
                            Title = txtExamTitle.Text,
                            DurationMinutes = int.Parse(txtDuration.Text),
                            TotalQuestions = total,
                            IsActive = true,

                            // --- CẬP NHẬT CÁC TRƯỜNG CÒN THIẾU ---
                            EasyCount = easyCount,
                            MediumCount = mediumCount,
                            HardCount = hardCount,
                            // --------------------------------------

                            ConfigMatrix = $"Auto: {DateTime.Now}"
                        };
                        context.Exams.Add(exam);
                        context.SaveChanges();

                        // Bước 4: Lưu vào bảng trung gian (ExamQuestions)
                        var examQuestionsList = new List<ExamQuestion>();
                        foreach (var q in allSelectedQuestions)
                        {
                            examQuestionsList.Add(new ExamQuestion
                            {
                                ExamId = exam.ExamId,
                                QuestionId = q.QuestionID
                            });
                        }

                        context.ExamQuestions.AddRange(examQuestionsList);
                        context.SaveChanges();

                        transaction.Commit();
                        MessageBox.Show($"Tạo đề thành công! (ID: {exam.ExamId})");
                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show("Lỗi: " + ex.Message);
                    }
                }
            }
        }
    }

    public class TopicSelectionViewModel : INotifyPropertyChanged
    {
        public int TopicId { get; set; }
        public string TopicName { get; set; }
        public int TotalAvailable { get; set; }
        private int _count;
        public int CountToSelect { get => _count; set { _count = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CountToSelect))); } }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}