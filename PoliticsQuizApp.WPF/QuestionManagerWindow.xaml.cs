using PoliticsQuizApp.WPF.Services;
using PoliticsQuizApp.WPF.ViewModels;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PoliticsQuizApp.WPF
{
    public partial class QuestionManagerWindow : Window
    {
        private QuestionService _service;
        private ObservableCollection<QuestionViewModel> _currentQuestions;
        private int _selectedTopicId = -1;

        public QuestionManagerWindow()
        {
            InitializeComponent();
            _service = new QuestionService();
            _currentQuestions = new ObservableCollection<QuestionViewModel>();
            dgQuestions.ItemsSource = _currentQuestions;
            LoadTopics();
        }

        private void LoadTopics() => lstTopics.ItemsSource = _service.GetTopics();

        private void LstTopics_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstTopics.SelectedItem == null) return;
            var topic = (dynamic)lstTopics.SelectedItem;
            _selectedTopicId = topic.TopicId;
            lblCurrentTopic.Text = topic.TopicName.ToUpper();
            RefreshQuestions();
        }

        private void RefreshQuestions()
        {
            if (_selectedTopicId == -1) return;
            var list = _service.GetQuestionsByTopic(_selectedTopicId);
            _currentQuestions.Clear();
            foreach (var item in list) _currentQuestions.Add(item);
            lblCount.Text = $"Tổng số câu: {_currentQuestions.Count}";
        }

        // --- SỬA LẠI: Dùng AddTopicWindow thay vì InputBox ---
        private void BtnAddTopic_Click(object sender, RoutedEventArgs e)
        {
            AddTopicWindow win = new AddTopicWindow();
            if (win.ShowDialog() == true)
            {
                string name = win.NewTopicName;
                bool success = _service.AddNewTopic(name);

                if (success)
                {
                    LoadTopics();
                    MessageBox.Show("Thêm chương thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Thông báo lỗi cụ thể hơn
                    MessageBox.Show($"Không thể thêm chương '{name}'.\n\nNguyên nhân có thể do:\n1. Tên bị trùng với chương đã có.\n2. Tên chứa ký tự không hợp lệ.",
                                    "Lỗi Thêm Chương", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // --- XÓA CHƯƠNG ---
        private void BtnDeleteTopic_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTopicId == -1)
            {
                MessageBox.Show("Vui lòng chọn một chương ở danh sách bên trên để xóa!", "Chưa chọn chương", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string topicName = lblCurrentTopic.Text;

            // Cảnh báo mạnh
            var result = MessageBox.Show($"⚠️ CẢNH BÁO QUAN TRỌNG!\n\nBạn đang yêu cầu xóa chương: '{topicName}'\n\nHành động này sẽ XÓA VĨNH VIỄN chương này và TẤT CẢ CÂU HỎI thuộc về nó.\n\nBạn có chắc chắn muốn tiếp tục không?",
                                         "Xác nhận Xóa Chương", MessageBoxButton.YesNo, MessageBoxImage.Error);

            if (result == MessageBoxResult.Yes)
            {
                bool success = _service.DeleteTopic(_selectedTopicId);

                if (success)
                {
                    MessageBox.Show("Đã xóa chương thành công!", "Thông báo");

                    // Reset trạng thái giao diện
                    _selectedTopicId = -1;
                    lblCurrentTopic.Text = "Vui lòng chọn chương...";
                    _currentQuestions.Clear();
                    lblCount.Text = "Tổng số câu: 0";

                    // Tải lại danh sách chương
                    LoadTopics();
                }
                else
                {
                    MessageBox.Show("Lỗi khi xóa chương. Có thể chương đang được sử dụng trong một bài thi nào đó.", "Lỗi hệ thống");
                }
            }
        }

        private void BtnAddQuestion_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTopicId == -1) { MessageBox.Show("Chọn chương trước!"); return; }
            // Truyền ID chương vào để tự chọn trong ComboBox
            AddQuestionWindow win = new AddQuestionWindow(_selectedTopicId);
            if (win.ShowDialog() == true) RefreshQuestions();
        }

        private void BtnDeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            var selected = _currentQuestions.Where(x => x.IsSelectedInManager).ToList();
            if (selected.Count == 0) return;
            if (MessageBox.Show($"Xóa {selected.Count} câu?", "Xác nhận", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                var ids = selected.Select(x => x.QuestionData.QuestionID).ToList();
                if (_service.DeleteMultipleQuestions(ids)) RefreshQuestions();
            }
        }

        private void BtnDeleteAllInTopic_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTopicId == -1) return;
            if (MessageBox.Show("Xóa TẤT CẢ câu hỏi chương này?", "Cảnh báo", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                if (_service.DeleteAllQuestionsInTopic(_selectedTopicId)) RefreshQuestions();
            }
        }
    }
}