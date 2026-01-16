using System;
using System.Collections.Generic;

namespace PoliticsQuizApp.Data.Models;

public partial class Question
{
    public long QuestionID { get; set; }

    public int TopicId { get; set; }

    public string Content { get; set; } = null!;

    public byte QuestionType { get; set; }

    public byte Difficulty { get; set; }

    public bool? IsShuffleAllowed { get; set; }

    public int? CreatedBy { get; set; }

    public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();

    public virtual Topic Topic { get; set; } = null!;
}
