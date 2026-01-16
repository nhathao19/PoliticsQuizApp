using Microsoft.Win32; // Để dùng OpenFileDialog
using OfficeOpenXml;
using PoliticsQuizApp.WPF.Services;
using System.Windows;

namespace PoliticsQuizApp.WPF
{
    public partial class ImportWindow : Window
    {
        private ImportService _importService;

        public ImportWindow()
        {
            InitializeComponent();
            _importService = new ImportService();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        // --- ĐÂY LÀ HÀM BẠN ĐANG THIẾU ---
        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Excel Files|*.xlsx"; // Chỉ cho chọn file Excel

            if (openFileDialog.ShowDialog() == true)
            {
                txtFilePath.Text = openFileDialog.FileName;
                btnImport.IsEnabled = true; // Cho phép bấm nút Import
            }
        }

        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            btnImport.IsEnabled = false; // Khóa nút để tránh bấm nhiều lần
            lblStatus.Text = "Đang xử lý...";

            // Chạy import
            string result = _importService.ImportFromExcel(txtFilePath.Text);

            MessageBox.Show(result, "Kết quả Import");
            this.Close();
        }
    }
}