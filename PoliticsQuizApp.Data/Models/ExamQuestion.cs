using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PoliticsQuizApp.Data.Models
{
    // Bảng này đóng vai trò "Cầu nối"
    public class ExamQuestion
    {
        [Key]
        public int Id { get; set; }

        public int ExamId { get; set; } // ID Đề thi

        public long QuestionId { get; set; } // ID Câu hỏi

        // Liên kết ngược lại với 2 bảng chính
        [ForeignKey("ExamId")]
        public virtual Exam Exam { get; set; }

        [ForeignKey("QuestionId")]
        public virtual Question Question { get; set; }
    }
}