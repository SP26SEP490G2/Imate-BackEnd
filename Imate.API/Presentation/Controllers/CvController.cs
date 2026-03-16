using Imate.API.Business.Interfaces;
using Imate.API.Common.Router;
using Imate.API.Presentation.RequestModels.UserManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Imate.API.Presentation.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize]
    public class CvController : ControllerBase
    {
        private readonly ICvService _cvService;

        public CvController(ICvService cvService)
        {
            _cvService = cvService;
        }

        /// <summary>
        /// Upload CV file (PDF, DOC, DOCX - max 5MB)
        /// </summary>
        [HttpPost(APIConfig.CV.Upload)]
        public async Task<IActionResult> UploadCv([FromForm] UploadCvRequestModel request)
        {
            try
            {
                var accountId = GetAccountId();
                if (accountId == null)
                    return Unauthorized(new { message = "Không thể xác định thông tin người dùng." });

                var cv = await _cvService.UploadCvAsync(accountId.Value, request.File, request.FileName);

                return Ok(new
                {
                    cvId = cv.Id.ToString(),
                    fileName = cv.FileName,
                    uploadDate = cv.UploadDate.ToString("o"),
                    status = "Valid"
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Get list of CVs for current user
        /// </summary>
        [HttpGet(APIConfig.CV.GetList)]
        public async Task<IActionResult> GetListCv()
        {
            try
            {
                var accountId = GetAccountId();
                if (accountId == null)
                    return Unauthorized(new { message = "Không thể xác định thông tin người dùng." });

                var cvList = await _cvService.GetListCvAsync(accountId.Value);

                var result = cvList.Select(cv => new
                {
                    cvId = cv.Id.ToString(),
                    fileName = cv.FileName,
                    uploadDate = cv.UploadDate.ToString("o"),
                    fileUrl = cv.FileUrl,
                    status = "Valid"
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Delete a CV by ID
        /// </summary>
        [HttpDelete(APIConfig.CV.Delete)]
        public async Task<IActionResult> DeleteCv(int cvId)
        {
            try
            {
                var accountId = GetAccountId();
                if (accountId == null)
                    return Unauthorized(new { message = "Không thể xác định thông tin người dùng." });

                await _cvService.DeleteCvAsync(accountId.Value, cvId);

                return Ok(new { message = "Xóa CV thành công." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Helper: Get accountId from JWT claims
        /// </summary>
        private int? GetAccountId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst("accountId")?.Value;

            if (claim != null && int.TryParse(claim, out int accountId))
                return accountId;

            return null;
        }
    }
}
