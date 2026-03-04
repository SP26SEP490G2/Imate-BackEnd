using Imate.API.Business.Interfaces;
using Imate.API.Common.Router;
using Imate.API.Presentation.RequestModels;
using Microsoft.AspNetCore.Mvc;

namespace Imate.API.Presentation.Controllers
{
    [ApiController]
    [Route("api")]
    public class QuestionController : ControllerBase
    {
        private readonly IQuestionService _questionService;

        public QuestionController(IQuestionService questionService)
        {
            _questionService = questionService;
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
                var categories = await _questionService.GetListQuestionCategoriesAsync();
                return Ok(new
                {
                    data = categories
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
    }
}
