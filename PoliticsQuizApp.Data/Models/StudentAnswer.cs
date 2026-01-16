using System;
using System.Collections.Generic;

namespace PoliticsQuizApp.Data.Models;

public partial class StudentAnswer
{
    public long Id { get; set; }

    public long SessionId { get; set; }

    public long QuestionId { get; set; }

    public long? SelectedAnswerId { get; set; }

    public bool? IsFlagged { get; set; }

    public DateTime? RecordedAt { get; set; }

    public virtual Question Question { get; set; } = null!;

    public virtual TestSession Session { get; set; } = null!;
}
