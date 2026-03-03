namespace Imate.API.Presentation.ResponseModels.UserManagement
{
    public class UserCvResponseModel
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; }
        public string? ScannedData { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

