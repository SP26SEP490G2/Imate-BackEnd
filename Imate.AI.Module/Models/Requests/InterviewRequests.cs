using Microsoft.AspNetCore.Http;

namespace Imate.AI.Module.Models.Requests
{
    /// <summary>
    /// Yêu cầu tạo phiên phỏng vấn mới
    /// </summary>
    public class CreateInterviewSessionRequest
    {
        public string? PositionName { get; set; }
        public string? SkillName { get; set; }
        public string[]? SkillNames { get; set; }
        public string? LevelName { get; set; }
        public string? CompanyName { get; set; }
        public string? JobDescriptionText { get; set; }
        public int? CvId { get; set; }
        public string? CvContent { get; set; }
        public string? Language { get; set; }
    }

    /// <summary>
    /// Yêu cầu tạo câu hỏi tiếp theo
    /// </summary>
    public class GenerateQuestionRequest
    {
        public int InterviewSessionId { get; set; }
        public double? EstimatedAbility { get; set; }
    }

    /// <summary>
    /// Yêu cầu gửi câu trả lời
    /// </summary>
    public class SubmitAnswerRequest
    {
        public int InterviewSessionId { get; set; }
        public int InterviewResponseId { get; set; }
        public string UserAnswer { get; set; } = string.Empty;
    }

    /// <summary>
    /// Yêu cầu thiết lập phỏng vấn — AI phân loại JD
    /// UC-34: Setup Interview
    /// </summary>
    public class SetupInterviewRequest
    {
        public string Method { get; set; } = "jd";
        public string? JobDescriptionSourceType { get; set; }
        public string? JobDescriptionText { get; set; }
        public string? JobDescriptionUrl { get; set; }
        public int? CvId { get; set; }
        public string? Language { get; set; }
        public IFormFile? File { get; set; }
    }
}
