namespace Imate.AI.Module.Models.Requests
{
    /// <summary>
    /// Request model cho phân tích CV bằng AI
    /// </summary>
    public class AnalyseCvRequest
    {
        /// <summary>
        /// CV ID từ database (optional - nếu muốn phân tích CV đã upload)
        /// </summary>
        public int? CvId { get; set; }

        /// <summary>
        /// Raw CV text trực tiếp (optional - dùng cho mock/testing)
        /// </summary>
        public string? CvText { get; set; }

        /// <summary>
        /// Nếu true, bỏ qua cache và phân tích lại từ đầu
        /// </summary>
        public bool ForceReanalyze { get; set; } = false;
    }
}
