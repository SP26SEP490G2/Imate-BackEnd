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

            var question = await _questionService.CreateSystemQuestionForStaffAsync(request);

            // Create audit log
            var userId = GetCurrentAccountId();
            if (userId.HasValue)
            {
                await _auditLogService.CreateAuditLogAsync(
                    userId.Value,
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
            }

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
            var question = await _questionService.GetSystemQuestionByIdAsync(questionId);

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
    }
}
