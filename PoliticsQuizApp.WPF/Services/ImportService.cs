using OfficeOpenXml; // Thư viện EPPlus
using PoliticsQuizApp.Data.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text; // Thêm cái này để dùng StringBuilder

namespace PoliticsQuizApp.WPF.Services
{
    public class ImportService
    {
        private readonly QuestionService _questionService;

        public ImportService()
        {
            _questionService = new QuestionService();
        }

        public string ImportFromExcel(string filePath)
        {
            int successCount = 0;
            int errorCount = 0;

            // Dùng cái này để ghi lại nhật ký lỗi chi tiết
            StringBuilder errorLog = new StringBuilder();

            try
            {
                // Đảm bảo file tồn tại
                if (!File.Exists(filePath)) return "Lỗi: File không tồn tại.";

                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    if (package.Workbook.Worksheets.Count == 0)
                        return "Lỗi: File Excel không có Sheet nào.";

                    var worksheet = package.Workbook.Worksheets[0];

                    if (worksheet.Dimension == null)
                        return "Lỗi: Sheet rỗng, không có dữ liệu.";

                    int rowCount = worksheet.Dimension.Rows;

                    // Bắt đầu duyệt từ dòng 2
                    for (int row = 2; row <= rowCount; row++)
                    {
                        try
                        {
                            // 1. Đọc dữ liệu (Thêm .Trim() để xóa khoảng trắng thừa)
                            string content = worksheet.Cells[row, 1].Text?.Trim();
                            string topicIdStr = worksheet.Cells[row, 2].Text?.Trim();
                            string diffStr = worksheet.Cells[row, 3].Text?.Trim();
                            string ansA = worksheet.Cells[row, 4].Text?.Trim();
                            string ansB = worksheet.Cells[row, 5].Text?.Trim();
                            string ansC = worksheet.Cells[row, 6].Text?.Trim();
                            string ansD = worksheet.Cells[row, 7].Text?.Trim();
                            string correctChar = worksheet.Cells[row, 8].Text?.Trim().ToUpper();

                            // Validate: Nếu nội dung trống thì coi như dòng trống -> bỏ qua
                            if (string.IsNullOrWhiteSpace(content)) continue;

                            // 2. Validate dữ liệu quan trọng
                            // Kiểm tra ID Chương
                            if (!int.TryParse(topicIdStr, out int topicId))
                            {
                                throw new Exception($"Mã chương (Cột 2) '{topicIdStr}' không phải là số.");
                            }

                            // Kiểm tra Độ khó
                            if (!byte.TryParse(diffStr, out byte diff))
                            {
                                diff = 1; // Mặc định là 1 nếu lỗi, không cần throw
                            }

                            // Kiểm tra đáp án đúng (Phải là A, B, C hoặc D)
                            if (correctChar != "A" && correctChar != "B" && correctChar != "C" && correctChar != "D")
                            {
                                throw new Exception($"Đáp án đúng (Cột 8) là '{correctChar}' không hợp lệ. Phải là A, B, C hoặc D.");
                            }

                            // 3. Tạo đối tượng Question
                            var q = new Question
                            {
                                Content = content,
                                TopicId = topicId,
                                Difficulty = diff,
                                QuestionType = 1,
                                CreatedBy = 1,
                                IsShuffleAllowed = true
                            };

                            var answers = new List<Answer>
                            {
                                new Answer { Content = ansA, IsCorrect = (correctChar == "A") },
                                new Answer { Content = ansB, IsCorrect = (correctChar == "B") },
                                new Answer { Content = ansC, IsCorrect = (correctChar == "C") },
                                new Answer { Content = ansD, IsCorrect = (correctChar == "D") }
                            };

                            // 4. Gọi Service lưu vào DB
                            if (_questionService.AddQuestion(q, answers))
                            {
                                successCount++;
                            }
                            else
                            {
                                // Nếu AddQuestion trả về false, thường là do lỗi Database (VD: TopicId không tồn tại)
                                throw new Exception("Lỗi lưu Database. Có thể Mã Chương không tồn tại trong hệ thống.");
                            }
                        }
                        catch (Exception ex)
                        {
                            errorCount++;
                            // Ghi lại lỗi cụ thể của dòng này
                            errorLog.AppendLine($"- Dòng {row}: {ex.Message}");
                        }
                    }
                }

                // Tổng kết báo cáo
                string msg = $"Hoàn tất!\n✅ Thành công: {successCount}\n❌ Thất bại: {errorCount}";

                if (errorCount > 0)
                {
                    msg += "\n\n=== CHI TIẾT LỖI ===\n" + errorLog.ToString();
                }

                return msg;
            }
            catch (Exception ex)
            {
                return "Lỗi nghiêm trọng khi đọc file: " + ex.Message;
            }
        }
    }
}