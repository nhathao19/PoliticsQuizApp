using Microsoft.EntityFrameworkCore;
using PoliticsQuizApp.Data;
using PoliticsQuizApp.Data.Models;
using PoliticsQuizApp.WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PoliticsQuizApp.WPF.Services
{
    public class ExamService
    {
        private readonly PoliticsQuizDbContext _context;

        public ExamService()
        {
            _context = new PoliticsQuizDbContext();
        }

        // ==========================================
        // PHẦN 1: DÀNH CHO ADMIN (QUẢN LÝ ĐỀ THI)
        // ==========================================

        // 1. Lấy danh sách tất cả đề thi
        public List<Exam> GetAllExams()
        {
            return _context.Exams
                           .OrderByDescending(e => e.ExamId)
                           .ToList();
        }

        // 2. Lấy danh sách Chủ đề
        public List<Topic> GetAllTopics()
        {
            return _context.Topics.ToList();
        }

        // 3. Thêm đề thi mới (Cấu hình ma trận câu hỏi)
        public bool AddExam(string code, string title, int? topicId, int duration, int easy, int medium, int hard)
        {
            try
            {
                if (_context.Exams.Any(e => e.ExamCode == code)) return false;

                var exam = new Exam
                {
                    ExamCode = code,
                    Title = title,
                    DurationMinutes = duration,
                    TopicID = topicId,

                    // Cấu hình ma trận
                    EasyCount = easy,
                    MediumCount = medium,
                    HardCount = hard,
                    TotalQuestions = easy + medium + hard,

                    IsActive = false
                };

                _context.Exams.Add(exam);
                _context.SaveChanges();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // 4. Bật/Tắt trạng thái đề thi
        public bool ToggleExamStatus(int examId)
        {
            try
            {
                var exam = _context.Exams.Find(examId);
                if (exam == null) return false;

                // Đảo ngược trạng thái
                exam.IsActive = !(exam.IsActive ?? false);
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // 5. Xóa đề thi
        public bool DeleteExam(int examId)
        {
            try
            {
                var exam = _context.Exams.Find(examId);
                if (exam == null) return false;

                _context.Exams.Remove(exam);
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ==========================================
        // PHẦN 2: DÀNH CHO SINH VIÊN (LÀM BÀI)
        // ==========================================
        public Exam GetExamById(int examId)
        {
            return _context.Exams.FirstOrDefault(x => x.ExamId == examId);
        }
        public List<Question> GetQuestionsByExamId(int examId)
        {
            using (var context = new PoliticsQuizDbContext())
            {
                // Logic: Vào bảng ExamQuestions -> Lấy Question -> Lấy luôn Answers
                var questions = context.ExamQuestions
                                       .Where(eq => eq.ExamId == examId)
                                       .Include(eq => eq.Question)
                                          .ThenInclude(q => q.Answers)
                                       .Select(eq => eq.Question)
                                       .ToList();
                return questions;
            }
        }
        public Exam GetExamByCode(string examCode)
        {
            return _context.Exams.FirstOrDefault(e => e.ExamCode == examCode);
        }

        public List<QuestionViewModel> GenerateExamQuestions(int examId, int v)
        {
            // 1. Lấy thông tin đề thi
            var exam = _context.Exams.Find(examId);
            if (exam == null) return new List<QuestionViewModel>();

            List<Question> finalQuestions = new List<Question>();

            // ==========================================================
            // BƯỚC 1: KIỂM TRA XEM ĐỀ CÓ CÂU HỎI CỐ ĐỊNH KHÔNG? (Ưu tiên)
            // ==========================================================
            // Logic này hỗ trợ Đề Thủ Công (Manual) - Lấy đúng những câu đã chọn
            var fixedQuestions = GetQuestionsByExamId(examId);

            if (fixedQuestions != null && fixedQuestions.Count > 0)
            {
                finalQuestions = fixedQuestions;
            }
            else
            {
                // ==========================================================
                // BƯỚC 2: NẾU KHÔNG CÓ CÂU CỐ ĐỊNH -> RANDOM (Auto)
                // ==========================================================

                // Dùng (exam.EasyCount ?? 0) để tránh lỗi crash nếu giá trị là null
                int eCount = exam.EasyCount ?? 0;
                int mCount = exam.MediumCount ?? 0;
                int hCount = exam.HardCount ?? 0;

                // -- Lấy câu DỄ --
                if (eCount > 0)
                {
                    var qs = _context.Questions
                        .Where(q => q.Difficulty == 1 && (exam.TopicID == null || q.TopicId == exam.TopicID))
                        .OrderBy(x => Guid.NewGuid())
                        .Take(eCount)
                        .Include(q => q.Answers).ToList();
                    finalQuestions.AddRange(qs);
                }

                // -- Lấy câu TRUNG BÌNH --
                if (mCount > 0)
                {
                    var qs = _context.Questions
                        .Where(q => q.Difficulty == 2 && (exam.TopicID == null || q.TopicId == exam.TopicID))
                        .OrderBy(x => Guid.NewGuid())
                        .Take(mCount)
                        .Include(q => q.Answers).ToList();
                    finalQuestions.AddRange(qs);
                }

                // -- Lấy câu KHÓ --
                if (hCount > 0)
                {
                    var qs = _context.Questions
                        .Where(q => q.Difficulty == 3 && (exam.TopicID == null || q.TopicId == exam.TopicID))
                        .OrderBy(x => Guid.NewGuid())
                        .Take(hCount)
                        .Include(q => q.Answers).ToList();
                    finalQuestions.AddRange(qs);
                }
            }

            // ==========================================================
            // BƯỚC 3: CHUYỂN ĐỔI SANG VIEWMODEL ĐỂ HIỂN THỊ
            // ==========================================================
            var viewModels = new List<QuestionViewModel>();
            int index = 1;

            // Trộn ngẫu nhiên vị trí câu hỏi một lần nữa trước khi hiện
            finalQuestions = finalQuestions.OrderBy(x => Guid.NewGuid()).ToList();

            foreach (var q in finalQuestions)
            {
                // Đảm bảo Answers không bị null
                var finalAnswers = q.Answers != null ? q.Answers.ToList() : new List<Answer>();

                // Xáo trộn đáp án
                finalAnswers = finalAnswers.OrderBy(a => Guid.NewGuid()).ToList();

                viewModels.Add(new QuestionViewModel
                {
                    QuestionData = q,
                    Index = index++,
                    Answers = finalAnswers
                });
            }
            return viewModels;
        }
        public List<QuestionViewModel> GenerateExamQuestions(int examId, int v)
        {
            // 1. Lấy thông tin đề thi
            var exam = _context.Exams.Find(examId);
            if (exam == null) return new List<QuestionViewModel>();

            List<Question> finalQuestions = new List<Question>();

            // ==========================================================
            // BƯỚC 1: KIỂM TRA XEM ĐỀ CÓ CÂU HỎI CỐ ĐỊNH KHÔNG? (Ưu tiên)
            // ==========================================================
            // Logic này hỗ trợ Đề Thủ Công (Manual) - Lấy đúng những câu đã chọn
            var fixedQuestions = GetQuestionsByExamId(examId);

            if (fixedQuestions != null && fixedQuestions.Count > 0)
            {
                finalQuestions = fixedQuestions;
            }
            else
            {
                // ==========================================================
                // BƯỚC 2: NẾU KHÔNG CÓ CÂU CỐ ĐỊNH -> RANDOM (Auto)
                // ==========================================================

                // Dùng (exam.EasyCount ?? 0) để tránh lỗi crash nếu giá trị là null
                int eCount = exam.EasyCount ?? 0;
                int mCount = exam.MediumCount ?? 0;
                int hCount = exam.HardCount ?? 0;

                // -- Lấy câu DỄ --
                if (eCount > 0)
                {
                    var qs = _context.Questions
                        .Where(q => q.Difficulty == 1 && (exam.TopicID == null || q.TopicId == exam.TopicID))
                        .OrderBy(x => Guid.NewGuid())
                        .Take(eCount)
                        .Include(q => q.Answers).ToList();
                    finalQuestions.AddRange(qs);
                }

                // -- Lấy câu TRUNG BÌNH --
                if (mCount > 0)
                {
                    var qs = _context.Questions
                        .Where(q => q.Difficulty == 2 && (exam.TopicID == null || q.TopicId == exam.TopicID))
                        .OrderBy(x => Guid.NewGuid())
                        .Take(mCount)
                        .Include(q => q.Answers).ToList();
                    finalQuestions.AddRange(qs);
                }

                // -- Lấy câu KHÓ --
                if (hCount > 0)
                {
                    var qs = _context.Questions
                        .Where(q => q.Difficulty == 3 && (exam.TopicID == null || q.TopicId == exam.TopicID))
                        .OrderBy(x => Guid.NewGuid())
                        .Take(hCount)
                        .Include(q => q.Answers).ToList();
                    finalQuestions.AddRange(qs);
                }
            }

            // ==========================================================
            // BƯỚC 3: CHUYỂN ĐỔI SANG VIEWMODEL ĐỂ HIỂN THỊ
            // ==========================================================
            var viewModels = new List<QuestionViewModel>();
            int index = 1;

            // Trộn ngẫu nhiên vị trí câu hỏi một lần nữa trước khi hiện
            finalQuestions = finalQuestions.OrderBy(x => Guid.NewGuid()).ToList();

            foreach (var q in finalQuestions)
            {
                // Đảm bảo Answers không bị null
                var finalAnswers = q.Answers != null ? q.Answers.ToList() : new List<Answer>();

                // Xáo trộn đáp án
                finalAnswers = finalAnswers.OrderBy(a => Guid.NewGuid()).ToList();

                viewModels.Add(new QuestionViewModel
                {
                    QuestionData = q,
                    Index = index++,
                    Answers = finalAnswers
                });
            }
            return viewModels;
        }
        public bool SubmitExam(int userId, int examId, double score, DateTime startTime, List<QuestionViewModel> questions)
        {
            using (var context = new PoliticsQuizDbContext())
            {
                try
                {
                    // 1. Lưu phiên thi (TestSession) - GIỮ NGUYÊN
                    var session = new TestSession
                    {
                        UserId = userId,
                        ExamId = examId,
                        StartTime = startTime,
                        EndTime = DateTime.Now,
                        Score = score
                    };
                    context.TestSessions.Add(session);
                    context.SaveChanges();

                    // 2. Lưu chi tiết bài làm (StudentAnswer) - SỬA ĐOẠN NÀY
                    foreach (var q in questions)
                    {
                        // Nếu sinh viên không chọn gì cả -> Bỏ qua hoặc lưu null
                        if (q.UserSelectedAnswerIds == null || q.UserSelectedAnswerIds.Count == 0) continue;

                        // Với mỗi đáp án được chọn -> Lưu 1 dòng vào DB
                        foreach (var ansId in q.UserSelectedAnswerIds)
                        {
                            var studentAns = new StudentAnswer
                            {
                                SessionId = session.SessionId,
                                QuestionId = q.QuestionData.QuestionID,
                                SelectedAnswerId = ansId, // Lưu từng ID một
                                IsFlagged = q.IsFlagged
                            };
                            context.StudentAnswers.Add(studentAns);
                        }
                    }

                    context.SaveChanges();
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        // ==========================================
        // PHẦN 3: BÁO CÁO & LỊCH SỬ (ĐÂY LÀ PHẦN BỊ THIẾU)
        // ==========================================
        public List<TestResultViewModel> GetExamHistory()
        {
            // Dùng LINQ để nối bảng: TestSessions + Users + Exams
            var query = from session in _context.TestSessions
                        join user in _context.Users on session.UserId equals user.UserId
                        join exam in _context.Exams on session.ExamId equals exam.ExamId
                        orderby session.StartTime descending
                        select new TestResultViewModel
                        {
                            SessionID = session.SessionId,
                            StudentName = user.FullName,
                            ExamTitle = exam.Title,
                            Score = (double)(session.Score ?? 0),
                            StartTime = (DateTime)(session.StartTime ?? DateTime.Now)
                        };

            return query.ToList();
        }
       
    }
}