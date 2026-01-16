using Microsoft.EntityFrameworkCore;
using PoliticsQuizApp.Data;
using PoliticsQuizApp.Data.Models;
using System.Collections.Generic;
using System.Linq;

namespace PoliticsQuizApp.WPF.Services
{
    public class QuestionService
    {
        private readonly PoliticsQuizDbContext _context;

        public QuestionService()
        {
            _context = new PoliticsQuizDbContext();
        }

        // Hàm lấy danh sách câu hỏi kèm theo Tên môn học và Tên chương
        public List<Question> GetAllQuestions()
        {
            // Sử dụng .Include để nối bảng (Join) lấy thông tin liên quan
            return _context.Questions
                           .Include(q => q.Topic)
                           .ThenInclude(t => t.Subject)
                           .AsNoTracking()
                           .OrderByDescending(q => q.QuestionID) // Câu mới nhất lên đầu
                           .ToList();
        }
        // 1. Hàm lấy danh sách Chủ đề (để đổ vào ComboBox)
        public List<Topic> GetTopics()
        {
            return _context.Topics.ToList();
        }

        // 2. Hàm Thêm câu hỏi mới và các đáp án
        public bool AddQuestion(Question newQuestion, List<Answer> answers)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // Bước 1: Lưu câu hỏi trước để lấy ID
                    _context.Questions.Add(newQuestion);
                    _context.SaveChanges(); // Lúc này newQuestion.QuestionID sẽ được sinh ra

                    // Bước 2: Gán ID câu hỏi cho các đáp án và lưu đáp án
                    foreach (var ans in answers)
                    {
                        ans.QuestionId = newQuestion.QuestionID;
                        _context.Answers.Add(ans);
                    }
                    _context.SaveChanges();

                    // Nếu mọi thứ ổn thì xác nhận lưu
                    transaction.Commit();
                    return true;
                }
                catch (System.Exception)
                {
                    // Nếu có lỗi thì hoàn tác, không lưu gì cả
                    transaction.Rollback();
                    return false;
                }
            }
        }
        public Topic AddTopic(string topicName)
        {
            var newTopic = new Topic
            {
                TopicName = topicName,
                SubjectId = 1 // Mặc định gán vào môn Chính trị (ID=1) để đơn giản hóa
            };
            _context.Topics.Add(newTopic);
            _context.SaveChanges();
            return newTopic;
        }
        public bool DeleteQuestion(long questionId)
        {
            try
            {
                var q = _context.Questions.Find(questionId);
                if (q == null) return false;

                // Xóa câu hỏi (Do Cascade Delete đã cấu hình, các đáp án con sẽ tự bay màu theo)
                _context.Questions.Remove(q);
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }
        public bool UpdateQuestionContent(long questionId, string newContent)
        {
            try
            {
                var q = _context.Questions.Find(questionId);
                if (q == null) return false;

                q.Content = newContent;
                // Nếu muốn sửa cả đáp án thì cần logic phức tạp hơn, 
                // tạm thời ta làm sửa nội dung câu hỏi trước.

                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }
        // 3. Hàm lấy chi tiết câu hỏi kèm theo đáp án
        public Question GetQuestionDetail(long questionId)
        {
            // Dùng Include để lấy luôn danh sách Answers đi kèm
            return _context.Questions
                           .Include(q => q.Answers)
                           .Include(q => q.Topic)
                           .FirstOrDefault(q => q.QuestionID == questionId);
        }
        // 4. Hàm cập nhật Full thông tin
        public bool UpdateQuestionFull(long qId, string content, int topicId, byte difficulty, List<string> answerTexts, int correctAnswerIndex)
        {
            try
            {
                var q = _context.Questions.Include(x => x.Answers).FirstOrDefault(x => x.QuestionID == qId);
                if (q == null) return false;

                // Cập nhật thông tin chính
                q.Content = content;
                q.TopicId = topicId;
                q.Difficulty = difficulty;

                // Cập nhật đáp án (Giả sử luôn có 4 đáp án)
                // Convert List answerTexts sang Array cho dễ truy xuất
                var arrTexts = answerTexts.ToArray();
                var arrAnswers = q.Answers.ToList();

                // Nếu số lượng đáp án trong DB ít hơn hoặc nhiều hơn 4 thì xóa đi tạo lại cho an toàn
                // Nhưng để đơn giản, ta giả định cấu trúc DB đã chuẩn (4 đáp án)
                for (int i = 0; i < arrAnswers.Count && i < 4; i++)
                {
                    arrAnswers[i].Content = arrTexts[i];

                    // Nếu index trùng với đáp án đúng -> Set true, ngược lại false
                    arrAnswers[i].IsCorrect = (i == correctAnswerIndex);
                }

                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}