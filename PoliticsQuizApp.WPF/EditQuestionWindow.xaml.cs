using PoliticsQuizApp.Data.Models; // Để dùng model Topic
using PoliticsQuizApp.WPF.Services;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PoliticsQuizApp.WPF
{
    public partial class EditQuestionWindow : Window
    {
        private long _questionId;
        private QuestionService _questionService;
        private TopicService _topicService;

        // Constructor chỉ cần nhận ID là đủ, vì ta sẽ query lại DB để lấy detail
        public EditQuestionWindow(long questionId)
        {
            InitializeComponent();
            _questionService = new QuestionService();
            _topicService = new TopicService();
            _questionId = questionId;

            LoadInitialData();
        }

        private void LoadInitialData()
        {
            // 1. Load ComboBox Topics
            cboTopics.ItemsSource = _topicService.GetAllTopics();

            // 2. Load thông tin câu hỏi
            var q = _questionService.GetQuestionDetail(_questionId);
            if (q != null)
            {
                txtContent.Text = q.Content;
                cboTopics.SelectedValue = q.TopicId;

                // Set độ khó (Trừ 1 vì index bắt đầu từ 0, còn Tag/Value ta đang set logic riêng)
                // Cách an toàn nhất là check tag
                foreach (ComboBoxItem item in cboDifficulty.Items)
                {
                    if (item.Tag.ToString() == q.Difficulty.ToString())
                    {
                        cboDifficulty.SelectedItem = item;
                        break;
                    }
                }

                // 3. Load 4 Đáp án lên giao diện
                // Lưu ý: List Answers có thể không theo thứ tự, ta cứ load lần lượt
                var answers = q.Answers.ToList();
                if (answers.Count >= 4)
                {
                    txtAnsA.Text = answers[0].Content;
                    radA.IsChecked = answers[0].IsCorrect;

                    txtAnsB.Text = answers[1].Content;
                    radB.IsChecked = answers[1].IsCorrect;

                    txtAnsC.Text = answers[2].Content;
                    radC.IsChecked = answers[2].IsCorrect;

                    txtAnsD.Text = answers[3].Content;
                    radD.IsChecked = answers[3].IsCorrect;
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Validate sơ bộ
            if (string.IsNullOrWhiteSpace(txtContent.Text) || cboTopics.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng nhập nội dung và chọn chủ đề!");
                return;
            }

            // Lấy độ khó
            var selectedDiffItem = cboDifficulty.SelectedItem as ComboBoxItem;
            byte difficulty = byte.Parse(selectedDiffItem.Tag.ToString());

            // Lấy Topic ID
            int topicId = (int)cboTopics.SelectedValue;

            // Gom 4 đáp án vào List
            List<string> newAnswers = new List<string>
            {
                txtAnsA.Text, txtAnsB.Text, txtAnsC.Text, txtAnsD.Text
            };

            // Tìm xem ô nào được check (0=A, 1=B, 2=C, 3=D)
            int correctIndex = 0;
            if (radB.IsChecked == true) correctIndex = 1;
            if (radC.IsChecked == true) correctIndex = 2;
            if (radD.IsChecked == true) correctIndex = 3;

            // Gọi Service update
            bool result = _questionService.UpdateQuestionFull(_questionId, txtContent.Text, topicId, difficulty, newAnswers, correctIndex);

            if (result)
            {
                MessageBox.Show("Cập nhật thành công!");
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Có lỗi xảy ra khi lưu!");
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}