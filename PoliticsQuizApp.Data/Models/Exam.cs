using System;
using System.Collections.Generic;

namespace PoliticsQuizApp.Data.Models;

public partial class Exam
{
    public int ExamId { get; set; }
    public string ExamCode { get; set; } = null!;
    public string Title { get; set; } = null!;
    public int DurationMinutes { get; set; }
    public int TotalQuestions { get; set; }
    public string? ConfigMatrix { get; set; }

    public virtual ICollection<TestSession> TestSessions { get; set; } = new List<TestSession>();

    public int? TopicID { get; set; }
    public int? EasyCount { get; set; }
    public int? MediumCount { get; set; }
    public int? HardCount { get; set; }
    public bool? IsActive { get; set; }
    public string DifficultySummary
    {
        get
        {
            int e = EasyCount ?? 0;
            int m = MediumCount ?? 0;
            int h = HardCount ?? 0;
            return $"{e}/{m}/{h}"; // Kết quả sẽ là: "5/3/2"
        }
    }
}
