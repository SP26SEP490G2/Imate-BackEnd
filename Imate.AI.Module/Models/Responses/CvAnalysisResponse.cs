namespace Imate.AI.Module.Models.Responses
{
    /// <summary>
    /// Response model cho kết quả phân tích CV bằng AI
    /// </summary>
    public class CvAnalysisResponse
    {
        public int Score { get; set; }
        public string CandidateName { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string MarketFit { get; set; } = string.Empty;
        public string DetectedLevel { get; set; } = string.Empty;
        public ScoreBreakdownDto ScoreBreakdown { get; set; } = new();
        public List<CvInsightDto> Strengths { get; set; } = new();
        public List<CvInsightDto> Improvements { get; set; } = new();
        public List<InterviewQuestionDto> InterviewQuestions { get; set; } = new();
    }

    public class ScoreBreakdownDto
    {
        public int WorkExperience { get; set; }
        public int SkillsAndTech { get; set; }
        public int EducationAndCerts { get; set; }
        public int RealWorldProjects { get; set; }
        public int CvStructure { get; set; }
    }

    public class CvInsightDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class InterviewQuestionDto
    {
        public string Category { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
    }
}
