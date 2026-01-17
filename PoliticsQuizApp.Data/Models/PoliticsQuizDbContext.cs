using System;
using Microsoft.EntityFrameworkCore;
using PoliticsQuizApp.Data.Models;

namespace PoliticsQuizApp.Data
{
    public partial class PoliticsQuizDbContext : DbContext
    {
        public PoliticsQuizDbContext() { }

        public PoliticsQuizDbContext(DbContextOptions<PoliticsQuizDbContext> options)
            : base(options) { }

        public virtual DbSet<Answer> Answers { get; set; }
        public virtual DbSet<Exam> Exams { get; set; }
        public virtual DbSet<Question> Questions { get; set; }
        public virtual DbSet<ExamQuestion> ExamQuestions { get; set; }
        public virtual DbSet<StudentAnswer> StudentAnswers { get; set; }
        public virtual DbSet<Subject> Subjects { get; set; }
        public virtual DbSet<TestSession> TestSessions { get; set; }
        public virtual DbSet<Topic> Topics { get; set; }
        public virtual DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Giữ nguyên cấu hình kết nối đã chạy được của bạn
                optionsBuilder.UseSqlServer(@"Data Source=localhost\SQLEXPRESS;Initial Catalog=PoliticsQuizDB;Integrated Security=True;TrustServerCertificate=True");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 1. User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.UserId).HasColumnName("UserID");
                entity.Property(e => e.RoleId).HasColumnName("RoleID");
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            });

            // 2. Question (SỬA LỖI TẠI ĐÂY)
            modelBuilder.Entity<Question>(entity =>
            {
                entity.HasKey(e => e.QuestionID);
                entity.Property(e => e.QuestionID).HasColumnName("QuestionID");
                entity.Property(e => e.TopicId).HasColumnName("TopicID");
                entity.Property(e => e.CreatedBy).HasColumnName("CreatedBy"); // Map cột CreatedBy

                // Cấu hình Relationship với Topic
                entity.HasOne(d => d.Topic)
                    .WithMany(p => p.Questions)
                    .HasForeignKey(d => d.TopicId)
                    .OnDelete(DeleteBehavior.SetNull);

                // --- KHẮC PHỤC LỖI 'CreatedByNavigationUserId' ---
                // Chỉ định rõ ràng: "CreatedByNavigation" liên kết qua khóa ngoại "CreatedBy"
                entity.HasOne(d => d.CreatedByNavigation)
                    .WithMany(p => p.Questions)
                    .HasForeignKey(d => d.CreatedBy)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // 3. Answer
            modelBuilder.Entity<Answer>(entity =>
            {
                entity.HasKey(e => e.AnswerId);
                entity.Property(e => e.AnswerId).HasColumnName("AnswerID");
                entity.Property(e => e.QuestionId).HasColumnName("QuestionID");

                entity.HasOne(d => d.Question)
                    .WithMany(p => p.Answers)
                    .HasForeignKey(d => d.QuestionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 4. TestSession
            modelBuilder.Entity<TestSession>(entity =>
            {
                entity.HasKey(e => e.SessionId);
                // Sửa lại tên cột cho khớp SQL (trong SQL là SessionID, không phải TestSessionID)
                entity.Property(e => e.SessionId).HasColumnName("SessionID");

                entity.Property(e => e.UserId).HasColumnName("UserID");
                entity.Property(e => e.ExamId).HasColumnName("ExamID");
            });

            // 5. StudentAnswer
            modelBuilder.Entity<StudentAnswer>(entity =>
            {
                entity.HasKey(e => e.Id);
                // Sửa lại tên cột cho khớp SQL (trong SQL là ID)
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.SessionId).HasColumnName("SessionID");
                entity.Property(e => e.QuestionId).HasColumnName("QuestionID");
            });

            // 6. Exam
            modelBuilder.Entity<Exam>(entity =>
            {
                entity.HasKey(e => e.ExamId);
                entity.Property(e => e.ExamId).HasColumnName("ExamID");
            });

            // 7. Topic
            modelBuilder.Entity<Topic>(entity =>
            {
                entity.HasKey(e => e.TopicId);
                entity.Property(e => e.TopicId).HasColumnName("TopicID");
                entity.Property(e => e.SubjectId).HasColumnName("SubjectID");
            });

            // 8. Subject
            modelBuilder.Entity<Subject>(entity =>
            {
                entity.HasKey(e => e.SubjectId);
                entity.Property(e => e.SubjectId).HasColumnName("SubjectID");
            });
        }
    }
}