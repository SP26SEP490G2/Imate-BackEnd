using ClosedXML.Excel;
using Imate.API.Presentation.ResponseModels.QuestionBank;

namespace Imate.API.Business.Services
{
    public class ExcelTemplateGenerator
    {
        public static byte[] GenerateQuestionTemplate()
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Questions");

                // Định nghĩa headers
                var headers = new List<string>
                {
                    "Content",
                    "Difficulty",
                    "SampleAnswer",
                    "CategoryNames",
                    "SkillNames",
                    "PositionNames"
                };

                // Thêm header row với styling
                for (int i = 0; i < headers.Count; i++)
                {
                    var cell = worksheet.Cell(1, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }

                // Thêm example data
                var examples = new List<object[]>
                {
                    new object[]
                    {
                        "Bạn sẽ làm gì khi có xung đột với đồng nghiệp trong dự án?",
                        "Medium",
                        "Lắng nghe quan điểm của họ, tìm ra điểm chung và cùng hướng tới mục tiêu chung của team.",
                        "Behavioral",
                        "Soft Skills",
                        "Fullstack Developer"
                    },
                    new object[]
                    {
                        "Giải thích sự khác biệt giữa Interface và Abstract Class.",
                        "Easy",
                        "Interface chỉ chứa khai báo, Abstract Class có thể chứa cả khai báo và định nghĩa chi tiết.",
                        "Technical",
                        "Java, C#",
                        "Backend Developer, Frontend Developer"
                    }
                };

                for (int row = 0; row < examples.Count; row++)
                {
                    for (int col = 0; col < examples[row].Length; col++)
                    {
                        var cell = worksheet.Cell(row + 2, col + 1);
                        cell.Value = examples[row][col].ToString();
                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    }
                }

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();

                // Thêm instructions sheet
                var instructionSheet = workbook.Worksheets.Add("Instructions");
                instructionSheet.Cell(1, 1).Value = "HƯỚNG DẪN IMPORT CÂU HỎI";
                instructionSheet.Cell(1, 1).Style.Font.Bold = true;
                instructionSheet.Cell(1, 1).Style.Font.FontSize = 16;

                var instructions = new List<string>
                {
                    "",
                    "1. Content: Nội dung câu hỏi (Bắt buộc, không được trùng)",
                    "2. Difficulty: Mức độ (Easy, Medium, Hard)",
                    "3. SampleAnswer: Câu trả lời mẫu (Bắt buộc)",
                    "4. CategoryNames: Tên các thể loại, ngăn cách bằng dấu phẩy (VD: Behavioral, Technical)",
                    "5. SkillNames: Tên các kỹ năng, ngăn cách bằng dấu phẩy (VD: Java, C#, React)",
                    "6. PositionNames: Tên các vị trí, ngăn cách bằng dấu phẩy (VD: Frontend Developer, Backend Developer)",
                    "",
                    "LƯU Ý:",
                    "- Tên Category, Skill, Position phải tồn tại trong hệ thống",
                    "- Nội dung câu hỏi không được trùng lặp",
                    "- Mức độ chỉ được là: Easy, Medium, hoặc Hard",
                    "- Xem sheet 'Questions' để biết ví dụ"
                };

                for (int i = 0; i < instructions.Count; i++)
                {
                    var cell = instructionSheet.Cell(i + 2, 1);
                    cell.Value = instructions[i];
                    if (i == 0 || i == 7) // Empty rows
                        continue;
                    if (instructions[i].StartsWith("LƯU Ý:"))
                    {
                        cell.Style.Font.Bold = true;
                        cell.Style.Font.FontColor = XLColor.Red;
                    }
                }

                instructionSheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        public static byte[] GenerateSystemQuestionsExport(List<GetAllSystemQuestionsForStaffAsyncResponse> questions)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Questions");

                // Định nghĩa headers
                var headers = new List<string>
                {
                    "Content",
                    "Difficulty",
                    "SampleAnswer",
                    "CategoryNames",
                    "SkillNames",
                    "PositionNames"
                };

                // Thêm header row với styling
                for (int i = 0; i < headers.Count; i++)
                {
                    var cell = worksheet.Cell(1, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }

                // Thêm dữ liệu câu hỏi
                for (int row = 0; row < questions.Count; row++)
                {
                    var question = questions[row];
                    int excelRow = row + 2; // Bắt đầu từ row 2 (row 1 là header)

                    worksheet.Cell(excelRow, 1).Value = question.Content ?? string.Empty;
                    worksheet.Cell(excelRow, 2).Value = question.Difficulty?.ToString() ?? string.Empty;
                    worksheet.Cell(excelRow, 3).Value = question.SampleAnswer ?? string.Empty;
                    worksheet.Cell(excelRow, 4).Value = question.CategoriesName != null && question.CategoriesName.Any() 
                        ? string.Join(", ", question.CategoriesName) 
                        : string.Empty;
                    worksheet.Cell(excelRow, 5).Value = question.SkillsName != null && question.SkillsName.Any() 
                        ? string.Join(", ", question.SkillsName) 
                        : string.Empty;
                    worksheet.Cell(excelRow, 6).Value = question.PositionsName != null && question.PositionsName.Any() 
                        ? string.Join(", ", question.PositionsName) 
                        : string.Empty;

                    // Thêm border cho các cell
                    for (int col = 1; col <= headers.Count; col++)
                    {
                        worksheet.Cell(excelRow, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    }
                }

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }
    }
}
