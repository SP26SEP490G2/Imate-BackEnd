using System.ComponentModel.DataAnnotations;

namespace Imate.API.Presentation.RequestModels.UserManagement
{
    /// <summary>
    /// Dữ liệu nộp / cập nhật hồ sơ Mentor (bước 2 sau khi đăng ký).
    /// </summary>
    public class UpdateMentorProfileRequest
    {
        [Required]
        public string Bio { get; set; }

        [Required]
        [Phone]
        public string Phone { get; set; }

        /// <summary>
        /// Ngày sinh dạng yyyy-MM-dd (tùy chọn).
        /// </summary>
        public string? BirthDate { get; set; }

        [Range(0, int.MaxValue)]
        public int? PricePerSession { get; set; }

        [Required]
        public string BankAccountHolderName { get; set; }

        [Required]
        public string BankAccountNumber { get; set; }

        /// <summary>
        /// Mã hoặc tên ngân hàng (sẽ được map nội bộ nếu cần).
        /// </summary>
        [Required]
        public string BankCode { get; set; }

        public List<int> PositionIds { get; set; } = new List<int>();
        public List<int> SkillIds { get; set; } = new List<int>();
        public List<int> CompanyIds { get; set; } = new List<int>();

        [Range(0, 50)]
        public int? Yoe { get; set; }

        public IFormFile? CvFile { get; set; }
        public IFormFile? CertificateFile { get; set; }
    }
}
