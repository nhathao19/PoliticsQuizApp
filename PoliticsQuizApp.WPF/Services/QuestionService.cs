using Microsoft.EntityFrameworkCore;
using PoliticsQuizApp.Data;
using PoliticsQuizApp.Data.Models;
using PoliticsQuizApp.WPF.ViewModels;

namespace PoliticsQuizApp.WPF.Services
{
    public class QuestionService
    {
        private readonly PoliticsQuizDbContext _context;

        public QuestionService()
        {
            _context = new PoliticsQuizDbContext();
        }

        // ==========================================
        // PHẦN 1: CÁC HÀM LẤY DỮ LIỆU (READ)
        // ==========================================

        // Lấy tất cả câu hỏi (Dùng cho MainWindow cũ)
        public List<QuestionViewModel> GetAllQuestions()
        {
            using (var context = new PoliticsQuizDbContext())
            {
                var list = context.Questions
                                  .Include(q => q.Topic)
                                  .OrderByDescending(q => q.QuestionID)
                                  .ToList();

                return list.Select(q => new QuestionViewModel
                {
                    QuestionData = q,
                    IsSelectedInManager = false
                }).ToList();
            }
        }

        // Lấy danh sách chủ đề
        public List<Topic> GetTopics()
        {
            using (var context = new PoliticsQuizDbContext())
            {
                return context.Topics.ToList();
            }
        }

        // Lấy chi tiết 1 câu hỏi kèm đáp án (Dùng cho EditQuestionWindow)
        public Question GetQuestionDetail(long questionId)
        {
            using (var context = new PoliticsQuizDbContext())
            {
                return context.Questions
                              .Include(q => q.Answers)
                              .FirstOrDefault(q => q.QuestionID == questionId);
            }
        }

        // Lấy câu hỏi theo từng chương (Dùng cho QuestionManagerWindow)
        public List<QuestionViewModel> GetQuestionsByTopic(int topicId)
        {
            using (var context = new PoliticsQuizDbContext())
            {
                var questions = context.Questions
                                       .Where(q => q.TopicId == topicId)
                                       .OrderByDescending(q => q.QuestionID)
                                       .ToList();

                var result = new List<QuestionViewModel>();
                foreach (var q in questions)
                {
                    result.Add(new QuestionViewModel
                    {
                        QuestionData = q,
                        IsSelectedInManager = false
                    });
                }
                return result;
            }
        }

        // ==========================================
        // PHẦN 2: CÁC HÀM THÊM / SỬA (CREATE / UPDATE)
        // ==========================================

        // Thêm câu hỏi mới
        public bool AddQuestion(Question question, List<Answer> answers)
        {
            using (var context = new PoliticsQuizDbContext())
            {
                using (var transaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        context.Questions.Add(question);
                        context.SaveChanges();

                        foreach (var ans in answers)
                        {
                            ans.QuestionId = question.QuestionID;
                            context.Answers.Add(ans);
                        }

                        context.SaveChanges();
                        transaction.Commit();
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }

        // --- KHẮC PHỤC LỖI 2: HÀM SỬA CÂU HỎI ---
        public bool UpdateQuestionFull(Question updatedQ, List<Answer> newAnswers)
        {
            using (var context = new PoliticsQuizDbContext())
            {
                using (var transaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        // 1. Cập nhật thông tin câu hỏi
                        var dbQ = context.Questions.FirstOrDefault(q => q.QuestionID == updatedQ.QuestionID);
                        if (dbQ != null)
                        {
                            dbQ.Content = updatedQ.Content;
                            dbQ.TopicId = updatedQ.TopicId;
                            dbQ.Difficulty = updatedQ.Difficulty;
                            // Giữ nguyên người tạo và ngày tạo
                        }

                        // 2. Cập nhật đáp án (Chiến thuật: Xóa hết cũ -> Thêm mới)
                        // Cách này an toàn nhất để tránh lỗi ID hoặc sót đáp án
                        var oldAnswers = context.Answers.Where(a => a.QuestionId == updatedQ.QuestionID);
                        context.Answers.RemoveRange(oldAnswers);
                        context.SaveChanges();

                        // Thêm lại đáp án mới
                        foreach (var ans in newAnswers)
                        {
                            ans.QuestionId = updatedQ.QuestionID; // Gán lại ID câu hỏi cho chắc
                            ans.AnswerId = 0; // Reset ID để EF tự sinh ID mới
                            context.Answers.Add(ans);
                        }

                        context.SaveChanges();
                        transaction.Commit();
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }

        // Thêm Chủ đề / Chương mới
        public Topic AddTopic(string topicName)
        {
            using (var context = new PoliticsQuizDbContext())
            {
                var existing = context.Topics.FirstOrDefault(t => t.TopicName == topicName);
                if (existing != null) return existing;

                var newTopic = new Topic { TopicName = topicName };
                context.Topics.Add(newTopic);
                context.SaveChanges();
                return newTopic;
            }
        }

        public bool AddNewTopic(string topicName)
        {
            using (var context = new PoliticsQuizDbContext())
            {
                if (string.IsNullOrWhiteSpace(topicName)) return false;

                string cleanName = topicName.Trim();

                // Kiểm tra trùng tên chương
                bool isExist = context.Topics.Any(t => t.TopicName.ToLower() == cleanName.ToLower());
                if (isExist) return false;

                // --- BẮT ĐẦU ĐOẠN SỬA LỖI ---

                // 1. Tìm môn học mặc định đầu tiên trong DB
                var defaultSubject = context.Subjects.FirstOrDefault();

                // 2. Nếu chưa có môn nào (DB trống), tạo môn mặc định ngay
                if (defaultSubject == null)
                {
                    defaultSubject = new Subject { SubjectName = "Môn Chính Trị Tổng Hợp" };
                    context.Subjects.Add(defaultSubject);
                    context.SaveChanges(); // Lưu để sinh SubjectID
                }

                // 3. Tạo Topic và gán SubjectId
                var newTopic = new Topic
                {
                    TopicName = cleanName,
                    SubjectId = defaultSubject.SubjectId
                };

                context.Topics.Add(newTopic);
                context.SaveChanges();
                return true;

                // --- KẾT THÚC ĐOẠN SỬA LỖI ---
            }
        }



        // ==========================================
        // PHẦN 3: CÁC HÀM XÓA (DELETE)
        // ==========================================

        // Xóa nhiều câu hỏi cùng lúc
        public bool DeleteMultipleQuestions(List<long> questionIds)
        {
            using (var context = new PoliticsQuizDbContext())
            {
                try
                {
                    // Xóa đáp án trước
                    var answers = context.Answers.Where(a => questionIds.Contains(a.QuestionId));
                    context.Answers.RemoveRange(answers);

                    // Xóa câu hỏi
                    var questions = context.Questions.Where(q => questionIds.Contains(q.QuestionID));
                    context.Questions.RemoveRange(questions);

                    context.SaveChanges();
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        // --- KHẮC PHỤC LỖI 1: HÀM XÓA 1 CÂU HỎI ---
        public bool DeleteQuestion(long questionId)
        {
            // Tận dụng hàm xóa nhiều để xóa 1 cái -> Code gọn hơn
            return DeleteMultipleQuestions(new List<long> { questionId });
        }

        // Xóa TẤT CẢ câu hỏi trong 1 chương
        public bool DeleteAllQuestionsInTopic(int topicId)
        {
            using (var context = new PoliticsQuizDbContext())
            {
                try
                {
                    var qIds = context.Questions
                                      .Where(q => q.TopicId == topicId)
                                      .Select(q => q.QuestionID)
                                      .ToList();

                    if (qIds.Count == 0) return true;

                    return DeleteMultipleQuestions(qIds);
                }
                catch { return false; }
            }
        }
        // 9. Xóa CHƯƠNG (Và toàn bộ câu hỏi bên trong)
        public bool DeleteTopic(int topicId)
        {
            using (var context = new PoliticsQuizDbContext())
            {
                using (var transaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        // Bước 1: Lấy danh sách ID các câu hỏi trong chương này
                        var qIds = context.Questions
                                          .Where(q => q.TopicId == topicId)
                                          .Select(q => q.QuestionID)
                                          .ToList();

                        if (qIds.Count > 0)
                        {
                            // Xóa tất cả đáp án của các câu hỏi đó
                            var answers = context.Answers.Where(a => qIds.Contains(a.QuestionId));
                            context.Answers.RemoveRange(answers);

                            // Xóa các câu hỏi
                            var questions = context.Questions.Where(q => qIds.Contains(q.QuestionID));
                            context.Questions.RemoveRange(questions);
                        }

                        // Bước 2: Xóa chính cái Chương đó
                        var topic = context.Topics.FirstOrDefault(t => t.TopicId == topicId);
                        if (topic != null)
                        {
                            context.Topics.Remove(topic);
                        }

                        context.SaveChanges();
                        transaction.Commit();
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }
    }
}