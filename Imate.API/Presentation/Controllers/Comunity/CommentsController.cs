using Microsoft.AspNetCore.Mvc;
using Imate.API.Business.Exceptions;
using Imate.API.Business.Interfaces.Comunity;
using Imate.API.Presentation.RequestModels.Comunity;
using System.Security.Claims;

namespace Imate.API.Presentation.Controllers.Comunity
{
    [ApiController]
    [Route("api")]
    // [Authorize] 
    public class CommentsController : ControllerBase
    {
        private readonly ICommentService _commentService;

        public CommentsController(ICommentService commentService)
        {
            _commentService = commentService;
        }

        [HttpPost("create-comment")]
        [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        // [Authorize]
        public async Task<IActionResult> CreateComment([FromBody] CreateCommentRequestModel request)
        {
            
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            //string userIdClaim = "302";
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Không tìm thấy thông tin người dùng hợp lệ." });
            }
            try
            {
                var commentId = await _commentService.CreateCommentAsync(userId, request);

                return StatusCode(StatusCodes.Status201Created);
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Không thể tạo comment.", error = ex.Message });
            }
        }

        [HttpPut("update-comment/{commentId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)] 
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)] 
        // [Authorize]
        public async Task<IActionResult> UpdateComment(int commentId, [FromBody] UpdateCommentRequestModel request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            //string userIdClaim = "302";
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Không tìm thấy thông tin người dùng hợp lệ." });
            }

            try
            {
                await _commentService.UpdateCommentAsync(commentId, userId, request);
                return NoContent();
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Bạn không có quyền chỉnh sửa bình luận này." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Không thể cập nhật bình luận.", error = ex.Message });
            }
        }

        [HttpPost("vote-comment/{commentId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> VoteComment(int commentId, [FromBody] VoteCommentRequestModel request)
        {
            // Lấy User ID từ token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            //string userIdClaim = "302";
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Không tìm thấy thông tin người dùng hợp lệ." });
            }

            try
            {
                await _commentService.ToggleVoteAsync(commentId, userId, request);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                // Trả về 403 Forbidden nếu cố gắng downvote bình luận của chính mình
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Không thể thực hiện vote.", error = ex.Message });
            }
        }

        [HttpDelete("delete-comment/{commentId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            //string userIdClaim = "302";
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Không tìm thấy thông tin người dùng hợp lệ." });
            }

            try
            {
                await _commentService.DeleteCommentAsync(commentId, userId);

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Không thể xóa bình luận.", error = ex.Message });
            }
        }

    }
}
