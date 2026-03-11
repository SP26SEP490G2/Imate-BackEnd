namespace Imate.API.Presentation.ResponseModels.QuestionBank
{
    // DTO chứa thông tin của một dòng câu hỏi sau khi validate
    public class QuestionValidationResponse
    {
        public int RowIndex { get; set; } // Số dòng trong file Excel để người dùng biết
        public string? Content { get; set; }
        public string? Difficulty { get; set; }
        public string? SampleAnswer { get; set; }
        public string? CategoryNames { get; set; } // Giữ dạng chuỗi để người dùng có thể sửa
        public string? SkillNames { get; set; }
        public string? PositionNames { get; set; }

        public bool IsValid { get; set; } = true; // Cờ báo hiệu dòng này có hợp lệ không
        public Dictionary<string, string> Errors { get; set; } = new Dictionary<string, string>(); // Chứa lỗi của từng trường
    }
}
