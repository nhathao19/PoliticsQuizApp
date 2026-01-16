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
        private void BtnToggleTopic_Click(object sender, RoutedEventArgs e)
        {
            _isAddingNewTopic = !_isAddingNewTopic; // Đảo trạng thái

            if (_isAddingNewTopic)
            {
                // Chuyển sang chế độ nhập mới
                cboTopics.Visibility = Visibility.Collapsed;
                txtNewTopic.Visibility = Visibility.Visible;
                txtNewTopic.Focus();
                btnToggleTopic.Content = "Hủy"; // Đổi nút thành Hủy
            }
            else
            {
                // Quay lại chọn list cũ
                cboTopics.Visibility = Visibility.Visible;
                txtNewTopic.Visibility = Visibility.Collapsed;
                txtNewTopic.Text = "";
                btnToggleTopic.Content = "+";
            }
        }
        private void LoadTopics()
        {
            cboTopics.ItemsSource = _service.GetTopics();
            cboTopics.SelectedIndex = 0; // Chọn mặc định cái đầu
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // 1. Validate dữ liệu đầu vào
            if (string.IsNullOrWhiteSpace(txtContent.Text))
            {
                MessageBox.Show("Vui lòng nhập nội dung câu hỏi!", "Thiếu thông tin");
                return;
            }

            // Kiểm tra kỹ xem đã chọn chủ đề chưa và giá trị có hợp lệ không
            if (cboTopics.SelectedItem == null || cboTopics.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn Chủ đề / Chương học!", "Thiếu thông tin");
                return;
            }

            // Kiểm tra đã chọn đáp án đúng chưa
            if (rbA.IsChecked == false && rbB.IsChecked == false &&
                rbC.IsChecked == false && rbD.IsChecked == false)
            {
                MessageBox.Show("Vui lòng chọn một đáp án đúng!", "Thiếu thông tin");
                return;
            }

            try
            {
                if (_isAddingNewTopic)
                {
                    if (string.IsNullOrWhiteSpace(txtNewTopic.Text))
                    {
                        MessageBox.Show("Vui lòng nhập tên chủ đề mới!", "Thiếu thông tin");
                        return;
                    }
                    // Gọi Service tạo chủ đề mới ngay lập tức
                    var newTopic = _service.AddTopic(txtNewTopic.Text);
                    selectedTopicId = newTopic.TopicId; // Lấy ID vừa sinh ra
                }
                else
                {
                    if (cboTopics.SelectedValue == null)
                    {
                        MessageBox.Show("Vui lòng chọn chủ đề!", "Thiếu thông tin");
                        return;
                    }
                    selectedTopicId = (int)cboTopics.SelectedValue;
                }
                // 2. Tạo đối tượng Question
                var question = new Question
                {
                    Content = txtContent.Text,
                    // Ép kiểu an toàn hơn
                    TopicId = selectedTopicId,
                    Difficulty = (byte)(cboDifficulty.SelectedIndex + 1),
                    QuestionType = 1,
                    CreatedBy = 1, // Admin
                    IsShuffleAllowed = true
                };

                // 3. Tạo danh sách Answers
                var answers = new List<Answer>
        {
            new Answer { Content = txtAnsA.Text, IsCorrect = rbA.IsChecked == true },
            new Answer { Content = txtAnsB.Text, IsCorrect = rbB.IsChecked == true },
            new Answer { Content = txtAnsC.Text, IsCorrect = rbC.IsChecked == true },
            new Answer { Content = txtAnsD.Text, IsCorrect = rbD.IsChecked == true }
        };

                // 4. Gọi Service lưu vào DB
                bool result = _service.AddQuestion(question, answers);

                if (result)
                {
                    MessageBox.Show("Thêm câu hỏi thành công!", "Thông báo");
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Có lỗi xảy ra khi lưu dữ liệu vào CSDL.", "Lỗi");
                }
            }
            catch (System.Exception ex)
            {
                // Bắt lỗi nếu có sự cố bất ngờ khác
                MessageBox.Show($"Lỗi chi tiết: {ex.Message}", "Lỗi Hệ Thống");
            }
        }
    }
}