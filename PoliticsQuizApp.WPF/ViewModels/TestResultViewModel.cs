namespace PoliticsQuizApp.WPF.ViewModels
{
    public class TestResultViewModel
    {
        public long SessionID { get; set; } // Để dùng khi muốn xem chi tiết
        public string StudentName { get; set; } // Họ tên (Thay vì ID)
        public string ExamTitle { get; set; }   // Tên đề/Chương
        public double Score { get; set; }       // Điểm số
        public DateTime StartTime { get; set; } // Ngày giờ thi

        // Tạo property hiển thị ngày giờ đẹp mắt
        public string FormattedDate => StartTime.ToString("dd/MM/yyyy HH:mm");

        // Tạo property hiển thị Trạng thái
        public string StatusText => "Đã nộp bài";
        public string StudentId { get; set; }
        public string ExamCode { get; set; }
    }
}