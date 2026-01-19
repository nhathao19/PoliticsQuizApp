using PoliticsQuizApp.Data.Models;
using PoliticsQuizApp.WPF.Services;
using System.Windows;

namespace PoliticsQuizApp.WPF
{
    public partial class EditQuestionWindow : Window
    {
        private QuestionService _service;
        private long _questionId;
        private Question _originalQuestion;

        public EditQuestionWindow(long questionId)
        {
            InitializeComponent();
            _service = new QuestionService();
            _questionId = questionId;

            LoadInitialData();
        }

        private void LoadInitialData()
        {
            // 1. Load danh sách chủ đề
            cboTopics.ItemsSource = _service.GetTopics();

            // 2. Load thông tin câu hỏi
            _originalQuestion = _service.GetQuestionDetail(_questionId);

            if (_originalQuestion != null)
            {
                // Fill thông tin cơ bản
                txtContent.Text = _originalQuestion.Content;
                cboTopics.SelectedValue = _originalQuestion.TopicId;
                if (cboDifficulty.Items.Count >= _originalQuestion.Difficulty)
                {
                    cboDifficulty.SelectedIndex = _originalQuestion.Difficulty - 1;
                }

                // Fill đáp án (Logic mới: Hỗ trợ nhiều đáp án đúng)
                var answers = _originalQuestion.Answers.ToList();
                if (answers.Count >= 4)
                {
                    txtAnsA.Text = answers[0].Content;
                    chkCorrectA.IsChecked = answers[0].IsCorrect;

                    txtAnsB.Text = answers[1].Content;
                    chkCorrectB.IsChecked = answers[1].IsCorrect;

                    txtAnsC.Text = answers[2].Content;
                    chkCorrectC.IsChecked = answers[2].IsCorrect;

                    txtAnsD.Text = answers[3].Content;
                    chkCorrectD.IsChecked = answers[3].IsCorrect;
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Validate
            if (string.IsNullOrWhiteSpace(txtContent.Text) || cboTopics.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng nhập đủ thông tin.");
                return;
            }

            if (chkCorrectA.IsChecked == false && chkCorrectB.IsChecked == false &&
                chkCorrectC.IsChecked == false && chkCorrectD.IsChecked == false)
            {
                MessageBox.Show("Vui lòng chọn ít nhất 1 đáp án đúng!");
                return;
            }

            // 1. Tạo đối tượng Question cần update
            var updatedQ = new Question
            {
                QuestionID = _questionId, // Quan trọng: Phải có ID để biết sửa câu nào
                Content = txtContent.Text,
                TopicId = (int)cboTopics.SelectedValue,
                Difficulty = (byte)(cboDifficulty.SelectedIndex + 1)
            };

            // 2. Tạo danh sách Answer mới (theo Checkbox)
            var newAnswers = new List<Answer>
            {
                new Answer { Content = txtAnsA.Text, IsCorrect = chkCorrectA.IsChecked == true },
                new Answer { Content = txtAnsB.Text, IsCorrect = chkCorrectB.IsChecked == true },
                new Answer { Content = txtAnsC.Text, IsCorrect = chkCorrectC.IsChecked == true },
                new Answer { Content = txtAnsD.Text, IsCorrect = chkCorrectD.IsChecked == true }
            };

            // 3. Gọi Service (Hàm này nhận đúng 2 tham số: Question và List<Answer>)
            bool result = _service.UpdateQuestionFull(updatedQ, newAnswers);

            if (result)
            {
                MessageBox.Show("Cập nhật thành công!");
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Lỗi khi cập nhật dữ liệu.");
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}