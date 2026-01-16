using Microsoft.Win32;
using PoliticsQuizApp.WPF.Services;
using PoliticsQuizApp.WPF.ViewModels;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;

namespace PoliticsQuizApp.WPF
{
    public partial class ReportWindow : Window
    {
        private ReportService _reportService;
        private ObservableCollection<TestResultViewModel> _currentDataObs;

        public ReportWindow()
        {
            InitializeComponent();
            _currentDataObs = new ObservableCollection<TestResultViewModel>();
            dgResults.ItemsSource = _currentDataObs;
            _reportService = new ReportService();

            RefreshData();
        }

        // --- 1. TẢI DỮ LIỆU & TÍNH TOÁN THỐNG KÊ ---
        private void RefreshData(string search = null)
        {
            var list = _reportService.GetResultsByExam(search);

            _currentDataObs.Clear();
            foreach (var item in list) _currentDataObs.Add(item);

            // TÍNH TOÁN THỐNG KÊ
            CalculateStats();
        }

        private void CalculateStats()
        {
            // 1. Tổng số bài thi
            if (txtTotalExams != null)
                txtTotalExams.Text = _currentDataObs.Count.ToString();

            // 2. Điểm trung bình
            if (txtAvgScore != null)
            {
                if (_currentDataObs.Count > 0)
                {
                    double avg = _currentDataObs.Average(x => x.Score);
                    txtAvgScore.Text = avg.ToString("N2"); // Làm tròn 2 số lẻ (VD: 8.50)
                }
                else
                {
                    txtAvgScore.Text = "0.00";
                }
            }
        }

        // --- 2. CÁC SỰ KIỆN NÚT BẤM ---

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            RefreshData(txtSearch.Text.Trim());
        }

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            RefreshData();
        }

        // Sự kiện: Xóa 1 kết quả (Nút thùng rác nhỏ trong bảng)
        private void BtnDeleteSingle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && int.TryParse(btn.Tag.ToString(), out int sessionId))
            {
                // Tìm tên sinh viên để hiện thông báo cho rõ
                var item = _currentDataObs.FirstOrDefault(x => x.SessionID == sessionId);
                string studentName = item != null ? item.StudentName : "thí sinh này";

                if (MessageBox.Show($"Bạn có chắc chắn muốn xóa kết quả thi của {studentName}?\nHành động này không thể hoàn tác.",
                                    "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    bool result = _reportService.DeleteResult(sessionId);
                    if (result)
                    {
                        RefreshData(txtSearch.Text.Trim()); // Tải lại dữ liệu
                        MessageBox.Show("Đã xóa thành công!", "Thông báo");
                    }
                    else
                    {
                        MessageBox.Show("Lỗi khi xóa dữ liệu!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        // Sự kiện: Xóa TẤT CẢ (Nút đỏ trên Toolbar)
        private void BtnDeleteAll_Click(object sender, RoutedEventArgs e)
        {
            if (_currentDataObs.Count == 0)
            {
                MessageBox.Show("Danh sách đang trống, không có gì để xóa.", "Thông báo");
                return;
            }

            // Hỏi xác nhận lần 1
            if (MessageBox.Show("CẢNH BÁO QUAN TRỌNG!\n\nBạn đang yêu cầu XÓA TOÀN BỘ KẾT QUẢ THI hiện có.\nĐiều này thường dùng để dọn dẹp hệ thống cho đợt thi mới.\n\nDữ liệu sau khi xóa sẽ KHÔNG THỂ KHÔI PHỤC.\nBạn có chắc chắn muốn tiếp tục?",
                                "Xác nhận xóa toàn bộ", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                // Hỏi xác nhận lần 2 cho chắc ăn
                if (MessageBox.Show("Xác nhận lần cuối: Bạn thực sự muốn xóa sạch dữ liệu kết quả thi?",
                                    "Xác nhận nghiêm trọng", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                {
                    bool result = _reportService.DeleteAllResults();
                    if (result)
                    {
                        RefreshData();
                        MessageBox.Show("Toàn bộ dữ liệu kết quả thi đã được dọn sạch.", "Hoàn tất");
                    }
                    else
                    {
                        MessageBox.Show("Có lỗi xảy ra khi xóa dữ liệu.", "Lỗi");
                    }
                }
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            if (_currentDataObs == null || _currentDataObs.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để xuất!");
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                FileName = $"KetQuaThi_{System.DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Chuyển ObservableCollection sang List để tương thích hàm Export cũ
                    _reportService.ExportResultsToExcel(new System.Collections.Generic.List<TestResultViewModel>(_currentDataObs), saveFileDialog.FileName);
                    MessageBox.Show("Xuất file thành công!");
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("Lỗi: " + ex.Message);
                }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnViewDetail_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && int.TryParse(btn.Tag.ToString(), out int sessionId))
            {
                ReviewWindow review = new ReviewWindow(sessionId);
                review.ShowDialog();
            }
        }
    }
}