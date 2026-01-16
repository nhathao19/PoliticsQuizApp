using System;
using System.Collections.Generic;

namespace PoliticsQuizApp.Data.Models;

public partial class Answer
{
    public long AnswerId { get; set; }

    public long QuestionId { get; set; }

    public string Content { get; set; } = null!;

    public bool IsCorrect { get; set; }

    public virtual Question Question { get; set; } = null!;
}
