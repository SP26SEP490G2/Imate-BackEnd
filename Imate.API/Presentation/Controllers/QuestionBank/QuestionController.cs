using Imate.API.Business.Exceptions;
using Imate.API.Business.Helper;
using Imate.API.Business.Interfaces;
using Imate.API.Business.Interfaces.QuestionBank;
using Imate.API.Business.Services;
using Imate.API.Common.Router;
using Imate.API.Models.Enums;
using Imate.API.Presentation.RequestModels.QuestionBank;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Imate.API.Presentation.Controllers.QuestionBank
{
    [ApiController]
    [Route("api")]
    public class QuestionController : ControllerBase
    {
        private readonly IQuestionService _questionService;
        private readonly IAuditLogService _auditLogService;

        public QuestionController(IQuestionService questionService, IAuditLogService auditLogService)
        {
            _questionService = questionService;
            _auditLogService = auditLogService;
        }

        private int? GetCurrentAccountId()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return null;
            }

            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (accountIdClaim != null && int.TryParse(accountIdClaim.Value, out int loggedInAccountId))
            {
                return loggedInAccountId;
            }

            return null;
        }

        [HttpGet(APIConfig.Question.GetListHotQuestions)]
        public async Task<IActionResult> GetListHotQuestions()
        {
            try
            {
                var questions = await _questionService.GetListHotQuestionsAsync();
                return Ok(new
                {
                    data = questions
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    data = (object?)null,
                    message = ex.Message
                });
            }
        }

        [HttpGet(APIConfig.Question.GetQuestionBankList)]
        public async Task<IActionResult> GetQuestionBankList([FromQuery] QuestionRequest.GetQuestionBankList request)
        {
            try
            {
                var result = await _questionService.GetQuestionBankListAsync(request);
                return Ok(new
                {
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    data = (object?)null,
                    message = ex.Message
                });
            }
        }

        [HttpGet(APIConfig.Question.GetListQuestionCategories)]
        public async Task<IActionResult> GetListQuestionCategories()
        {
            try
            {
                //var categories = await _questionService.GetListQuestionCategoriesAsync();
                return Ok(new
                {
                    data = ""
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    data = (object?)null,
                    message = ex.Message
                });
            }
        }
        [Authorize(Roles = "Staff, Admin")]
        [HttpGet(APIConfig.Question.GetAllSystemQuestionsForStaff)]
        public async Task<IActionResult> GetAllSystemQuestionsForStaffAsync([FromQuery] GetSystemQuestionParams questionParams)
        {
            var pagedResult = await _questionService.GetAllSystemQuestionsForStaffAsync(questionParams);
            Response.Headers.Add("X-Pagination",
                System.Text.Json.JsonSerializer.Serialize(
            new
            {
                pagedResult.TotalCount,
                pagedResult.PageSize,
                pagedResult.PageNumber,
                pagedResult.TotalPages
            }));

            return Ok(pagedResult);
        }
        [Authorize(Roles = "Staff, Admin")]
        [HttpGet(APIConfig.Question.GetAllContributedQuestionsForStaff)]
        public async Task<IActionResult> GetAllContributedQuestionsForStaffAsync([FromQuery] GetContributedQuestionParams questionParams)
        {
            var pagedResult = await _questionService.GetAllContributedQuestionsForStaffAsync(questionParams);
            Response.Headers.Add("X-Pagination",
                 System.Text.Json.JsonSerializer.Serialize(
             new
             {
                 pagedResult.TotalCount,
                 pagedResult.PageSize,
                 pagedResult.PageNumber,
                 pagedResult.TotalPages
             }));

            return Ok(pagedResult);
        }
        [Authorize(Roles = "Staff, Admin")]
        [HttpPost(APIConfig.Question.CreateSystemQuestionForStaff)]
        public async Task<IActionResult> CreateSystemQuestionForStaffAsync([FromBody] CreateSystemQuestionForStaffRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var accountIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(accountIdClaim, out var userId))
            {
                return Unauthorized("User ID is invalid.");
            }

            var question = await _questionService.CreateSystemQuestionForStaffAsync(request, userId);

            // Create audit log

            await _auditLogService.CreateAuditLogAsync(
                userId,
                AuditAction.Create,
                "Question",
                question.Id,
                null,
                new
                {
                    Content = question.Content,
                    Difficulty = question.Difficulty?.ToString(),
                    IsFromSystem = question.IsFromSystem,
                    IsActive = question.IsActive,
                    CreatorId = question.CreatorId
                }
            );

            return Ok(new
            {
                Message = "Tạo câu hỏi hệ thống cho staff thành công.",
                QuestionId = question.Id

            });
        }
        [Authorize(Roles = "Staff, Admin")]
        [HttpPut(APIConfig.Question.UpdateSystemQuestionForStaff)]
        public async Task<IActionResult> UpdateSystemQuestionForStaffAsync(int questionId, [FromBody] UpdateSystemQuestionForStaffRequest request)
        {

            if (!ModelState.IsValid)
            {

                return BadRequest(ModelState);
            }

            var updatedQuestion = await _questionService.UpdateSystemQuestionForStaffAsync(questionId, request);

            return Ok(new
            {
                Message = $"Cập nhật câu hỏi ID {questionId} thành công.",
                QuestionId = updatedQuestion.Id
            });
        }
        [HttpGet(APIConfig.Question.GetSystemQuestionById)]
        public async Task<IActionResult> GetSystemQuestionByIdAsync(int questionId)
        {
            var accountId = GetCurrentAccountId();
            var question = await _questionService.GetSystemQuestionByIdAsync(questionId, accountId);

            return Ok(question);
        }

        [HttpGet(APIConfig.Question.GetContributedQuestionById)]
        public async Task<IActionResult> GetContributedQuestionByIdAsync(int questionId)
        {
            var accountId = GetCurrentAccountId();
            var question = await _questionService.GetContributedQuestionByIdAsync(questionId, accountId);

            return Ok(question);
        }

        [HttpGet(APIConfig.Question.GetPublicSystemQuestionBanks)]
        public async Task<IActionResult> GetPublicSystemQuestionBanks([FromQuery] GetPublicSystemQuestionParams questionParams)
        {
            try
            {
                var accountId = GetCurrentAccountId();
                var subscription = User.FindFirstValue("SubscriptionPackage");

                // Nếu có pagination params, sử dụng endpoint mới với pagination
                if (questionParams.PageNumber > 0 && questionParams.PageSize > 0)
                {
                    var pagedResult = await _questionService.GetPublicSystemQuestionBanksWithPaginationAsync(subscription, accountId, questionParams);
                    Response.Headers.Add("X-Pagination",
                        System.Text.Json.JsonSerializer.Serialize(
                            new
                            {
                                pagedResult.TotalCount,
                                pagedResult.PageSize,
                                pagedResult.PageNumber,
                                pagedResult.TotalPages
                            }));
                    return Ok(new
                    {
                        success = true,
                        data = pagedResult,
                        message = "Lấy danh sách câu hỏi thành công"
                    });
                }
                else
                {
                    // Giữ lại endpoint cũ cho backward compatibility
                    var questions = await _questionService.GetPublicSystemQuestionBanksAsync(subscription, accountId);
                    return Ok(new
                    {
                        success = true,
                        data = questions,
                        message = "Lấy danh sách câu hỏi thành công" + subscription
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi lấy danh sách câu hỏi",
                    error = ex.Message
                });
            }
        }

        [HttpGet(APIConfig.Question.GetPublicContributedQuestionBanks)]
        public async Task<IActionResult> GetPublicContributedQuestionBanks([FromQuery] GetPublicContributedQuestionParams questionParams)
        {
            try
            {
                var accountId = GetCurrentAccountId();
                var subscription = User.FindFirstValue("SubscriptionPackage");

                // Nếu có pagination params, sử dụng endpoint mới với pagination
                if (questionParams.PageNumber > 0 && questionParams.PageSize > 0)
                {
                    var pagedResult = await _questionService.GetPublicContributedQuestionBanksWithPaginationAsync(subscription, accountId, questionParams);
                    Response.Headers.Add("X-Pagination",
                        System.Text.Json.JsonSerializer.Serialize(
                            new
                            {
                                pagedResult.TotalCount,
                                pagedResult.PageSize,
                                pagedResult.PageNumber,
                                pagedResult.TotalPages
                            }));
                    return Ok(new
                    {
                        success = true,
                        data = pagedResult,
                        message = "Lấy danh sách câu hỏi thành công"
                    });
                }
                else
                {
                    // Giữ lại endpoint cũ cho backward compatibility
                    var questions = await _questionService.GetAllPublicContributedQuestionAsync(subscription, accountId);
                    return Ok(new
                    {
                        success = true,
                        data = questions,
                        //total = questions.Count,
                        message = "Lấy danh sách câu hỏi thành công" + subscription
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi lấy danh sách câu hỏi",
                    error = ex.Message
                });
            }
        }

        [Authorize(Roles = "Candidate")]
        [HttpPost(APIConfig.Question.ContributeQuestion)]
        public async Task<IActionResult> ContributeQuestion([FromBody] ContributeQuestionRequestModel request)
        {
            var accountIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(accountIdClaim, out var userId))
            {
                return Unauthorized("User ID is invalid.");
            }

            await _questionService.CreateContributedQuestionAsync(request, userId);
            return StatusCode(201, new { message = "Your question has been contributed successfully!" });
        }

        [HttpGet(APIConfig.Question.ExportSystemQuestions)]
        public async Task<IActionResult> ExportSystemQuestionsAsync([FromQuery] GetSystemQuestionParams questionParams)
        {
            try
            {
                var fileBytes = await _questionService.ExportSystemQuestionsToExcelAsync(questionParams);

                // Đặt tên file với timestamp
                string fileName = $"System_Questions_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                // Trả về file với Content-Type chính xác
                return File(
                    fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to export questions: " + ex.Message });
            }
        }
        [Authorize(Roles = "Candidate")]
        [HttpGet(APIConfig.Question.GetMyContributedQuestions)]
        public async Task<IActionResult> GetMyContributedQuestionsAsync([FromQuery] GetMyContributedQuestionsParams questionParams)
        {
            try
            {
                var accountIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(accountIdClaim) || !int.TryParse(accountIdClaim, out int accountId))
                {
                    return Unauthorized(new { message = "Invalid user authentication." });
                }

                var pagedResult = await _questionService.GetMyContributedQuestionsAsync(accountId, questionParams);
                Response.Headers.Add("X-Pagination",
                    System.Text.Json.JsonSerializer.Serialize(
                        new
                        {
                            pagedResult.TotalCount,
                            pagedResult.PageSize,
                            pagedResult.PageNumber,
                            pagedResult.TotalPages
                        }));

                return Ok(pagedResult);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi lấy danh sách câu hỏi đóng góp của bạn",
                    error = ex.Message
                });
            }
        }
        [Authorize(Roles = "Staff, Admin")]
        [HttpGet(APIConfig.Question.GetAllPendingContributedQuestionsForStaff)]
        public async Task<IActionResult> GetAllPendingContributedQuestionForStaffAsync([FromQuery] PendingContributedParams questionParams)
        {
            var pagedResult = await _questionService.GetAllPendingContributedQuestionForStaffAsync(questionParams);
            Response.Headers.Add("X-Pagination",
                 System.Text.Json.JsonSerializer.Serialize(
             new
             {
                 pagedResult.TotalCount,
                 pagedResult.PageSize,
                 pagedResult.PageNumber,
                 pagedResult.TotalPages
             }));

            return Ok(pagedResult);
        }
        [Authorize(Roles = "Staff, Admin")]
        [HttpPut(APIConfig.Question.ChangeContributedQuestionStatusForStaff)]
        public async Task<IActionResult> UpdateContributedQuestionStatusAsync(int questionId, bool status)
        {
            try
            {
                var accountIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(accountIdString, out var staffId))
                {
                    return Unauthorized("Token không hợp lệ.");
                }

                var question = await _questionService.UpdateContributedQuestionStatusAsync(questionId, status, staffId);
                return Ok(new
                {
                    Message = $"Cập nhật trạng thái câu hỏi ID {questionId} thành công.",
                    QuestionId = question.Id,
                    NewStatus = question.IsActive
                });
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost(APIConfig.Question.ValidateQuestionsFromExcel)]
        public async Task<IActionResult> ValidateQuestionsFromExcel(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "Vui lòng chọn file để upload." });
                }

                var fileExtension = Path.GetExtension(file.FileName)?.ToLower();
                if (fileExtension != ".xlsx" && fileExtension != ".xls")
                {
                    return BadRequest(new { message = "Chỉ chấp nhận file Excel (.xlsx, .xls)." });
                }

                var validationResults = await _questionService.ValidateQuestionsFromExcelAsync(file);
                return Ok(validationResults);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message,
                    detail = "Vui lòng kiểm tra lại định dạng file Excel. File phải có đúng các cột: Content, Difficulty, SampleAnswer, CategoryNames, SkillNames, PositionNames"
                });
            }
        }

        [HttpPost(APIConfig.Question.ImportValidatedQuestions)]
        public async Task<IActionResult> ImportValidatedQuestions([FromBody] List<FinalImportRequest> requests)
        {
            try
            {
                // Lấy Creator ID từ JWT Token
                var accountIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(accountIdClaim) || !int.TryParse(accountIdClaim, out int creatorId))
                {
                    return Unauthorized(new { message = "Không thể xác thực người dùng. Vui lòng đăng nhập lại." });
                }

                if (requests == null || requests.Count == 0)
                {
                    return BadRequest(new { message = "Danh sách câu hỏi trống." });
                }

                var count = await _questionService.CreateValidatedQuestionsAsync(requests, creatorId);

                return Ok(new
                {
                    success = true,
                    message = $"Đã import thành công {count} câu hỏi.",
                    importedCount = count
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra trong quá trình import.",
                    detail = ex.Message
                });
            }
        }

        [HttpPost(APIConfig.Question.RevalidateSingleQuestion)]
        public async Task<IActionResult> RevalidateSingleQuestion([FromBody] FinalImportRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var validationResult = await _questionService.RevalidateSingleQuestionAsync(request);

                return Ok(validationResult);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi kiểm tra lại dữ liệu.",
                    detail = ex.Message
                });
            }
        }

        // Trong QuestionController.cs (hoặc tương tự)

        [HttpGet(APIConfig.Question.DownloadQuestionTemplate)]
        public IActionResult DownloadQuestionTemplate()
        {
            try
            {
                // Gọi hàm của bạn để lấy mảng byte
                var fileBytes = ExcelTemplateGenerator.GenerateQuestionTemplate();

                // Đặt tên file (đảm bảo tên file không chứa ký tự đặc biệt)
                string fileName = $"System_Question_Import_Template_{DateTime.Now:yyyyMMdd}.xlsx";

                // Trả về file với Content-Type chính xác
                return File(
                    fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName
                );
            }
            catch (Exception ex)
            {
                // Log lỗi và trả về lỗi 500
                return StatusCode(500, "Failed to generate template: " + ex.Message);
            }
        }
    }
}
