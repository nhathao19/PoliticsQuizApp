using OfficeOpenXml; // Thư viện EPPlus
using PoliticsQuizApp.Data.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
            var questionsToAdd = new List<Question>();
            var answersToAdd = new List<Answer>();
            int successCount = 0;
            int errorCount = 0;

            try
            {
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    // Kiểm tra xem file có sheet nào không
                    if (package.Workbook.Worksheets.Count == 0)
                        return "Lỗi: File Excel không có dữ liệu (Sheet rỗng).";

                    var worksheet = package.Workbook.Worksheets[0]; // Lấy sheet đầu tiên

                    if (worksheet.Dimension == null)
                        return "Lỗi: Sheet không có dữ liệu.";

                    int rowCount = worksheet.Dimension.Rows;

                    // Duyệt từ dòng 2 (vì dòng 1 thường là tiêu đề)
                    for (int row = 2; row <= rowCount; row++)
                    {
                        try
                        {
                            // 1. Đọc dữ liệu từng ô
                            string content = worksheet.Cells[row, 1].Text; // Cột 1: Nội dung
                            string topicIdStr = worksheet.Cells[row, 2].Text; // Cột 2: Mã chủ đề
                            string diffStr = worksheet.Cells[row, 3].Text; // Cột 3: Độ khó
                            string ansA = worksheet.Cells[row, 4].Text;
                            string ansB = worksheet.Cells[row, 5].Text;
                            string ansC = worksheet.Cells[row, 6].Text;
                            string ansD = worksheet.Cells[row, 7].Text;
                            string correctChar = worksheet.Cells[row, 8].Text.Trim().ToUpper(); // A, B, C, D

                            // Validate cơ bản: Nếu nội dung trống thì bỏ qua
                            if (string.IsNullOrWhiteSpace(content)) continue;

                            // Xử lý an toàn khi parse số
                            if (!int.TryParse(topicIdStr, out int topicId)) topicId = 1; // Mặc định 1 nếu lỗi
                            if (!byte.TryParse(diffStr, out byte diff)) diff = 1;

                            // 2. Tạo đối tượng Question
                            var q = new Question
                            {
                                Content = content,
                                TopicId = topicId,
                                Difficulty = diff,
                                QuestionType = 1, // Mặc định Single Choice
                                CreatedBy = 1, // Admin
                                IsShuffleAllowed = true
                            };

                            var answers = new List<Answer>
                            {
                                new Answer { Content = ansA, IsCorrect = (correctChar == "A") },
                                new Answer { Content = ansB, IsCorrect = (correctChar == "B") },
                                new Answer { Content = ansC, IsCorrect = (correctChar == "C") },
                                new Answer { Content = ansD, IsCorrect = (correctChar == "D") }
                            };

                            // Gọi hàm thêm câu hỏi có sẵn bên QuestionService
                            if (_questionService.AddQuestion(q, answers))
                            {
                                successCount++;
                            }
                            else
                            {
                                errorCount++;
                            }
                        }
                        catch (Exception)
                        {
                            errorCount++; // Dòng lỗi data
                        }
                    }
                }
                return $"Hoàn tất! Thành công: {successCount} câu. Lỗi: {errorCount} câu.";
            }
            catch (Exception ex)
            {
                return "Lỗi đọc file: " + ex.Message;
            }
        }
    }
}