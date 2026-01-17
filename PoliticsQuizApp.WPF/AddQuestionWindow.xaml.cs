using PoliticsQuizApp.Data.Models;
using PoliticsQuizApp.WPF.Services;
using System.Collections.Generic;
using System.Windows;

namespace PoliticsQuizApp.WPF
{
    public partial class AddQuestionWindow : Window
    {
        private QuestionService _service;
        private bool _isAddingNewTopic = false;
        int selectedTopicId;

        public AddQuestionWindow()
        {
            InitializeComponent();
            _service = new QuestionService();
            LoadTopics();
        }

        // Constructor nhận ID chương (để tự chọn khi mở từ Quản lý chương)
        public AddQuestionWindow(int topicId) : this()
        {
            cboTopics.SelectedValue = topicId;
        }

        private void BtnToggleTopic_Click(object sender, RoutedEventArgs e)
        {
            _isAddingNewTopic = !_isAddingNewTopic;
            if (_isAddingNewTopic)
            {
                cboTopics.Visibility = Visibility.Collapsed;
                txtNewTopic.Visibility = Visibility.Visible;
                txtNewTopic.Focus();
                btnToggleTopic.Content = "Hủy";
            }
            else
            {
                cboTopics.Visibility = Visibility.Visible;
                txtNewTopic.Visibility = Visibility.Collapsed;
                btnToggleTopic.Content = "+";
            }
        }

        private void LoadTopics()
        {
            cboTopics.ItemsSource = _service.GetTopics();
            cboTopics.SelectedIndex = 0;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtContent.Text)) { MessageBox.Show("Chưa nhập nội dung!"); return; }

            // Logic Checkbox: Phải chọn ít nhất 1
            if (chkA.IsChecked == false && chkB.IsChecked == false && chkC.IsChecked == false && chkD.IsChecked == false)
            {
                MessageBox.Show("Vui lòng tích chọn ít nhất một đáp án đúng!");
                return;
            }

            try
            {
                if (_isAddingNewTopic)
                {
                    if (string.IsNullOrWhiteSpace(txtNewTopic.Text)) return;
                    var newTopic = _service.AddTopic(txtNewTopic.Text);
                    selectedTopicId = newTopic.TopicId;
                }
                else
                {
                    if (cboTopics.SelectedValue == null) return;
                    selectedTopicId = (int)cboTopics.SelectedValue;
                }

                var question = new Question
                {
                    Content = txtContent.Text,
                    TopicId = selectedTopicId,
                    Difficulty = (byte)(cboDifficulty.SelectedIndex + 1),
                    QuestionType = 1,
                    CreatedBy = 1,
                    IsShuffleAllowed = true
                };

                // Lấy từ Checkbox
                var answers = new List<Answer>
                {
                    new Answer { Content = txtAnsA.Text, IsCorrect = chkA.IsChecked == true },
                    new Answer { Content = txtAnsB.Text, IsCorrect = chkB.IsChecked == true },
                    new Answer { Content = txtAnsC.Text, IsCorrect = chkC.IsChecked == true },
                    new Answer { Content = txtAnsD.Text, IsCorrect = chkD.IsChecked == true }
                };

                if (_service.AddQuestion(question, answers))
                {
                    MessageBox.Show("Thêm thành công!");
                    DialogResult = true;
                    Close();
                }
            }
            catch (System.Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
        }
    }
}