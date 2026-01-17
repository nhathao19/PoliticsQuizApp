using PoliticsQuizApp.WPF.Services;
using PoliticsQuizApp.WPF.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PoliticsQuizApp.WPF
{
    public partial class ReviewWindow : Window
    {
        private ReportService _reportService;
        private List<QuestionViewModel> _questions;
        private int _currentIndex = 0;

        public ReviewWindow(int sessionId)
        {
            InitializeComponent();
            _reportService = new ReportService();

            // Lấy dữ liệu bài thi cũ
            _questions = _reportService.GetReviewDetails(sessionId);

            icNavigator.ItemsSource = null;
            icNavigator.Items.Clear();

            icNavigator.ItemsSource = _questions;

            if (_questions.Count > 0) DisplayQuestion(0);
        }

        private void DisplayQuestion(int index)
        {
            if (index < 0 || index >= _questions.Count) return;

            // Update trạng thái chọn bên trái
            if (_questions[_currentIndex] != null) _questions[_currentIndex].IsSelected = false;
            _currentIndex = index;
            _questions[_currentIndex].IsSelected = true;

            var qVM = _questions[index];
            gridContent.DataContext = qVM;

            // Vẽ đáp án
            pnlAnswers.Children.Clear();
            foreach (var ans in qVM.Answers)
            {
                // Dùng Border bao quanh TextBlock để tô màu nền cho đẹp
                Border border = new Border
                {
                    Padding = new Thickness(10),
                    Margin = new Thickness(0, 5, 0, 5),
                    CornerRadius = new CornerRadius(5),
                    BorderThickness = new Thickness(1),
                    BorderBrush = Brushes.Gray
                };

                TextBlock txt = new TextBlock
                {
                    Text = ans.Content,
                    FontSize = 16,
                    TextWrapping = TextWrapping.Wrap
                };

                // --- LOGIC TÔ MÀU ---
                // 1. Nếu đây là đáp án ĐÚNG -> Luôn tô Xanh lá
                if (ans.IsCorrect)
                {
                    border.Background = new SolidColorBrush(Color.FromRgb(200, 230, 201)); // Xanh nhạt
                    border.BorderBrush = Brushes.Green;
                    txt.FontWeight = FontWeights.Bold;
                    txt.Text += " (ĐÁP ÁN ĐÚNG)";
                }

                // 2. Nếu đây là đáp án SV CHỌN
                if (qVM.UserSelectedAnswerIds.Contains(ans.AnswerId))
                {
                    // Nếu chọn ĐÚNG -> Đã tô xanh ở trên rồi -> Thêm icon
                    if (ans.IsCorrect)
                    {
                        txt.Text = "✅ " + txt.Text;
                    }
                    // Nếu chọn SAI -> Tô Đỏ
                    else
                    {
                        border.Background = new SolidColorBrush(Color.FromRgb(255, 205, 210)); // Đỏ nhạt
                        border.BorderBrush = Brushes.Red;
                        txt.Text = "❌ " + txt.Text + " (BẠN CHỌN)";
                    }
                }

                border.Child = txt;
                pnlAnswers.Children.Add(border);
            }
        }

        private void BtnNav_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is QuestionViewModel q)
                DisplayQuestion(q.Index - 1);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}