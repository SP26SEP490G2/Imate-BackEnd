namespace Imate.API.Presentation.ResponseModels.Applications
{
    public class ApplicationListResponse
    {

        // ID của đơn (để click vào xem chi tiết)
        public int Id { get; set; }

        // Cột "Loại đơn" (ví dụ: "Đơn Tố Cáo")
        public string ApplicationType { get; set; }

        // Cột "Ngày gửi" (Lấy từ BaseEntity.CreatedAt)
        public DateOnly CreatedAt { get; set; }
        public string Title { get; set; }

        // Cột "Nội dung" (Có thể cắt ngắn nếu cần)
        public string Content { get; set; }

        // Cột "Trạng thái" (ví dụ: "Chưa xử lý")
        public string Status { get; set; }

        // Cột "Ghi chú phản hồi"
        public string ResponseNote { get; set; }

        // Cột "Người phản hồi" (là một object con)
        public ReviewerInfoResponse? Reviewer { get; set; }

    }
    public class ReviewerInfoResponse
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string AvatarUrl { get; set; }
    }
}
