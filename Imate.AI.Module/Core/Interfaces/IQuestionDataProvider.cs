
namespace Imate.AI.Module.Core.Interfaces
{
    /// <summary>
    /// Interface bridge để AI Module truy cập Question Bank từ DB
    /// Host project (Imate.API) implement interface này
    /// </summary>
    public interface IQuestionDataProvider
    {
        /// <summary>
        /// Lấy câu hỏi từ ngân hàng câu hỏi theo vị trí và độ khó
        /// </summary>
        /// <param name="positionName">Tên vị trí (VD: "Backend Developer")</param>
        /// <param name="level">Cấp bậc (VD: "Junior", "Senior")</param>
        /// <param name="maxCount">Số câu hỏi tối đa cần lấy</param>
        Task<List<QuestionBankItem>> GetQuestionsAsync(string positionName, string level, int maxCount = 10);
    }

    /// <summary>
    /// DTO đại diện cho một câu hỏi từ ngân hàng câu hỏi
    /// </summary>
    public class QuestionBankItem
    {
        public string Content { get; set; } = string.Empty;
        public string? SampleAnswer { get; set; }
        public string Difficulty { get; set; } = string.Empty;
        public List<string> Skills { get; set; } = new();
        public List<string> Categories { get; set; } = new();
    }
}