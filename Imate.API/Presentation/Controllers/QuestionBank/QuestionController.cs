using Imate.API.Business.Exceptions;
using Imate.API.Business.Helper;
using Imate.API.Business.Interfaces;
using Imate.API.Business.Interfaces.QuestionBank;
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
public async Task<IActionResult> GetSystemQuestionForStaffByIdAsync(int questionId)
{
    var accountId = GetCurrentAccountId();
    var question = await _questionService.GetSystemQuestionByIdAsync(questionId, accountId);

    return Ok(question);
}

[HttpGet(APIConfig.Question.GetContributedQuestionById)]
public async Task<IActionResult> GetContributedQuestionForStaffByIdAsync(int questionId)
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
    }
}
