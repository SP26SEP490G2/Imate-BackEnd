using System.ComponentModel.DataAnnotations;

namespace Imate.API.Presentation.RequestModels
{
    public class MentorProfileRegistrationRequest
    {
        [Required(ErrorMessage = "Họ và tên không được để trống.")]
        [StringLength(100, ErrorMessage = "Họ và tên không được vượt quá 100 ký tự.")]
        public string FullName { get; set; }

        public IFormFile? AvatarFile { get; set; }

        [Required(ErrorMessage = "Tiểu sử không được để trống.")]
        public string Bio { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống.")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "Số điện thoại chỉ được chứa ký tự số.")]
        public string Phone { get; set; }

        public DateOnly? BirthDate { get; set; }

        [Range(0, 50, ErrorMessage = "Số năm kinh nghiệm phải từ 0 đến 50.")]
        public int Yoe { get; set; } 

        public IFormFile? CvFile { get; set; }
        public IFormFile? CertificateFile { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Giá mỗi phiên phải là một số dương.")]
        public int PricePerSession { get; set; }

        [Required(ErrorMessage = "Tên chủ tài khoản ngân hàng không được để trống.")]
        public string BankAccountHolderName { get; set; }

        [Required(ErrorMessage = "Số tài khoản ngân hàng không được để trống.")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "Số tài khoản ngân hàng chỉ được chứa ký tự số.")]
        public string BankAccountNumber { get; set; }

        [Required(ErrorMessage = "Ngân hàng không được để trống.")]
        public string BankCode { get; set; }
        public IEnumerable<int> SkillIds { get; set; } = new List<int>();
        public IEnumerable<int> PositionIds { get; set; } = new List<int>();
        public IEnumerable<int> CompanyIds { get; set; } = new List<int>();
    }
}
