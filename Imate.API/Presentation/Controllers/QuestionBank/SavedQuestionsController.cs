using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imate.API.Business.Interfaces.QuestionBank;
using Imate.API.Presentation.RequestModels.QuestionBank;
using System.Security.Claims;

namespace Imate.API.Presentation.Controllers.QuestionBank
{
    [ApiController]
    [Route("api")]
    //[Authorize]
    public class SavedQuestionsController : ControllerBase
    {
        private readonly ISavedQuestionService _savedQuestionService;

        public SavedQuestionsController(ISavedQuestionService savedQuestionService)
        {
            _savedQuestionService = savedQuestionService;
        }
        [HttpPost("save-question")]
        public async Task<IActionResult> ToggleSaveQuestion([FromBody] SaveQuestionRequestModel request)
        {
            try
            {
                // Lấy AccountId từ JWT token
                var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(accountIdClaim) || !int.TryParse(accountIdClaim, out int accountId))
                {
                    return Unauthorized(new { message = "Invalid user authentication." });
                }

                var result = await _savedQuestionService.ToggleSaveQuestionAsync(accountId, request.QuestionId);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while processing your request.", detail = ex.Message });
            }
        }

        [HttpGet("savedquestions-system")]
        public async Task<IActionResult> GetSavedSystemQuestions()
        {
            try
            {
                var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                //string accountIdClaim = "302";

                if (string.IsNullOrEmpty(accountIdClaim) || !int.TryParse(accountIdClaim, out int accountId))
                {
                    return Unauthorized(new { message = "Invalid user authentication." });
                }

                var questions = await _savedQuestionService.GetSavedSystemQuestionsAsync(accountId);

                return Ok(questions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving saved system questions.", detail = ex.Message });
            }
        }

        [HttpGet("savedquestions-contributed")]
        public async Task<IActionResult> GetSavedContributedQuestions()
        {
            try
            {
                var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(accountIdClaim) || !int.TryParse(accountIdClaim, out int accountId))
                {
                    return Unauthorized(new { message = "Invalid user authentication." });
                }

                var questions = await _savedQuestionService.GetSavedContributedQuestionsAsync(accountId);

                return Ok(questions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving saved contributed questions.", detail = ex.Message });
            }
        }
    }
}
