using System;
using System.Collections.Generic;

namespace PoliticsQuizApp.Data.Models;

public partial class TestSession
{
    public long SessionId { get; set; }

    public int UserId { get; set; }

    public int ExamId { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public double? Score { get; set; }

    public byte? Status { get; set; }

    public int? TimeRemaining { get; set; }

    public virtual Exam Exam { get; set; } = null!;

    public virtual ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();

    public virtual User User { get; set; } = null!;
}
