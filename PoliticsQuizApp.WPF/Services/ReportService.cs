using Microsoft.EntityFrameworkCore;
using OfficeOpenXml; 
using OfficeOpenXml.Style; 
using PoliticsQuizApp.Data;
using PoliticsQuizApp.WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PoliticsQuizApp.WPF.Services
{
    public class ReportService
    {
        private readonly PoliticsQuizDbContext _context;

        public ReportService()
        {
            _context = new PoliticsQuizDbContext();
            // Cấu hình License cho thư viện Excel (Bắt buộc)
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }
        // 1. Lấy danh sách kết quả thi
        public List<TestResultViewModel> GetResultsByExam(string examCode = null)
        {
            var query = from session in _context.TestSessions
                        join user in _context.Users on session.UserId equals user.UserId
                        join exam in _context.Exams on session.ExamId equals exam.ExamId
                        select new { session, user, exam };

            if (!string.IsNullOrEmpty(examCode))
            {
                query = query.Where(x => x.exam.ExamCode.Contains(examCode));
            }

            return query.OrderByDescending(x => x.session.StartTime)
                        .Select(x => new TestResultViewModel
                        {
                            SessionID = x.session.SessionId,
                            StudentName = x.user.FullName,
                            StudentId = x.user.Username,
                            ExamTitle = x.exam.Title,
                            ExamCode = x.exam.ExamCode,
                            Score = Math.Round((double)(x.session.Score ?? 0), 2),
                            StartTime = (DateTime)x.session.StartTime
                        }).ToList();
        }
        // 2. Hàm Xuất Excel (ĐÃ SỬA: Dùng IEnumerable để nhận được mọi loại danh sách)
        public void ExportResultsToExcel(IEnumerable<TestResultViewModel> data, string filePath)
        {
            // Tạo file Excel mới
            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                // Nếu sheet đã tồn tại thì xóa đi tạo lại để tránh lỗi
                var existingSheet = package.Workbook.Worksheets["KetQuaThi"];
                if (existingSheet != null)
                {
                    package.Workbook.Worksheets.Delete(existingSheet);
                }

                var ws = package.Workbook.Worksheets.Add("KetQuaThi");

                // --- HEADER ---
                ws.Cells[1, 1].Value = "MSSV";
                ws.Cells[1, 2].Value = "Họ Tên";
                ws.Cells[1, 3].Value = "Mã Đề";
                ws.Cells[1, 4].Value = "Tên Đề Thi";
                ws.Cells[1, 5].Value = "Điểm số";
                ws.Cells[1, 6].Value = "Ngày giờ nộp";

                // Format Header: In đậm, nền xanh, căn giữa
                using (var range = ws.Cells[1, 1, 1, 6])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                }

                // --- DATA ---
                // Dùng foreach thay vì for để tương thích với IEnumerable
                int rowIndex = 2;
                foreach (var item in data)
                {
                    ws.Cells[rowIndex, 1].Value = item.StudentId;
                    ws.Cells[rowIndex, 2].Value = item.StudentName;
                    ws.Cells[rowIndex, 3].Value = item.ExamCode;
                    ws.Cells[rowIndex, 4].Value = item.ExamTitle;

                    // Format điểm số: Căn giữa, in đậm nếu điểm cao
                    ws.Cells[rowIndex, 5].Value = item.Score;
                    ws.Cells[rowIndex, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    if (item.Score >= 8) ws.Cells[rowIndex, 5].Style.Font.Bold = true;

                    ws.Cells[rowIndex, 6].Value = item.StartTime.ToString("dd/MM/yyyy HH:mm");

                    rowIndex++;
                }

                // Tự động căn chỉnh độ rộng cột
                ws.Cells.AutoFitColumns();

                // Lưu file
                package.Save();
            }
        }
        // 3. Hàm lấy chi tiết bài làm (Cho chức năng Review)
        public List<QuestionViewModel> GetReviewDetails(int sessionId)
        {
            var studentAnswers = _context.StudentAnswers.Where(sa => sa.SessionId == sessionId).ToList();
            var questionIds = studentAnswers.Select(sa => sa.QuestionId).ToList();

            var questions = _context.Questions
                                    .Include(q => q.Answers)
                                    .Where(q => questionIds.Contains(q.QuestionID))
                                    .ToList();

            var viewModels = new List<QuestionViewModel>();
            int index = 1;

            foreach (var q in questions)
            {
                var userAns = studentAnswers.FirstOrDefault(sa => sa.QuestionId == q.QuestionID);
                var qVM = new QuestionViewModel
                {
                    QuestionData = q,
                    Index = index++,
                    Answers = q.Answers.ToList(),
                    UserSelectedAnswerId = userAns?.SelectedAnswerId ?? 0,
                    IsFlagged = userAns?.IsFlagged ?? false
                };

                var correctAns = q.Answers.FirstOrDefault(a => a.IsCorrect);
                if (correctAns != null && correctAns.AnswerId == qVM.UserSelectedAnswerId)
                {
                    qVM.IsAnswered = true; // Đúng
                }
                else
                {
                    qVM.IsAnswered = false; // Sai
                }
                viewModels.Add(qVM);
            }
            return viewModels;
        }
        // 4. Xóa một kết quả thi cụ thể
        public bool DeleteResult(int sessionId)
        {
            try
            {
                var session = _context.TestSessions.FirstOrDefault(s => s.SessionId == sessionId);
                if (session != null)
                {
                    // Xóa các câu trả lời chi tiết của bài thi này trước (để sạch data)
                    var answers = _context.StudentAnswers.Where(a => a.SessionId == sessionId);
                    _context.StudentAnswers.RemoveRange(answers);

                    // Sau đó xóa phiên thi
                    _context.TestSessions.Remove(session);
                    _context.SaveChanges();
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
        // 5. Xóa TẤT CẢ kết quả (Dọn dẹp cho đợt thi mới)
        public bool DeleteAllResults()
        {
            try
            {
                // Xóa toàn bộ câu trả lời
                var allAnswers = _context.StudentAnswers.ToList();
                _context.StudentAnswers.RemoveRange(allAnswers);

                // Xóa toàn bộ phiên thi
                var allSessions = _context.TestSessions.ToList();
                _context.TestSessions.RemoveRange(allSessions);

                _context.SaveChanges();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}