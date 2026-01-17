using PoliticsQuizApp.Data;
using PoliticsQuizApp.Data.Models;
using System;
using System.Collections.Generic; // Cần thiết để dùng List
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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
                        // 1. Tạo Đề Thi (Exam)
                        var exam = new Exam
                        {
                            ExamCode = txtExamCode.Text,
                            Title = txtExamTitle.Text,
                            DurationMinutes = int.Parse(txtDuration.Text),
                            TotalQuestions = total,
                            IsActive = true,
                            ConfigMatrix = $"Auto: {DateTime.Now}"
                        };
                        context.Exams.Add(exam);
                        context.SaveChanges(); // Lưu để lấy ExamId

                        // 2. Random câu hỏi và LƯU VÀO EXAMQUESTIONS
                        var examQuestionsList = new List<ExamQuestion>();

                        foreach (var t in _topics)
                        {
                            if (t.CountToSelect > 0)
                            {
                                var randomQ = context.Questions
                                                     .Where(q => q.TopicId == t.TopicId)
                                                     .OrderBy(x => Guid.NewGuid()) // Random
                                                     .Take(t.CountToSelect)
                                                     .ToList();

                                foreach (var q in randomQ)
                                {
                                    // Tạo liên kết trong bảng trung gian
                                    examQuestionsList.Add(new ExamQuestion
                                    {
                                        ExamId = exam.ExamId,
                                        QuestionId = q.QuestionID
                                    });
                                }
                            }
                        }

                        // Lưu danh sách liên kết vào DB
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