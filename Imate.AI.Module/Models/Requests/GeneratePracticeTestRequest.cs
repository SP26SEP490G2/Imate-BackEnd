namespace Imate.AI.Module.Models.Requests
{
    /// <summary>
    /// Request cho việc sinh bài test luyện tập bằng AI
    /// UC-30: Practice Test
    /// </summary>
    public class GeneratePracticeTestRequest
    {
        /// <summary>
        /// Loại bài test: "Technical" hoặc "Language"
        /// </summary>
        public string TestType { get; set; } = "Technical";

        /// <summary>
        /// Lĩnh vực chuyên môn: "Frontend Developer", "Backend Developer", "Fullstack", etc.
        /// </summary>
        public string Field { get; set; } = string.Empty;

        /// <summary>
        /// Cấp bậc ứng tuyển: "Intern", "Fresher", "Junior", "Middle", "Senior"
        /// </summary>
        public string Level { get; set; } = "Junior";

        /// <summary>
        /// Có cá nhân hóa theo CV hay không
        /// </summary>
        public bool UseCV { get; set; } = false;

        /// <summary>
        /// Nội dung CV (optional - nếu UseCV = true)
        /// </summary>
        public string? CvText { get; set; }

        /// <summary>
        /// Số lượng câu hỏi mong muốn (mặc định 10)
        /// </summary>
        public int NumberOfQuestions { get; set; } = 10;
    }
}
