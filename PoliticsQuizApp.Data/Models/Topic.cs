using System;
using System.Collections.Generic;

namespace PoliticsQuizApp.Data.Models;

public partial class Topic
{
    public int TopicId { get; set; }

    public int SubjectId { get; set; }

    public string TopicName { get; set; } = null!;

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

    public virtual Subject Subject { get; set; } = null!;
}
