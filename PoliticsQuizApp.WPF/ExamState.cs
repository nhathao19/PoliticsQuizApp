public class ExamTempState
{
    public int ExamId { get; set; }
    public double TimeRemainingSeconds { get; set; }
    public List<StudentAnswerTemp> Answers { get; set; } // Danh sách câu trả lời
}

public class StudentAnswerTemp
{
    public long QuestionId { get; set; }
    public long? SelectedAnswerId { get; set; }
    public bool IsFlagged { get; set; }
}