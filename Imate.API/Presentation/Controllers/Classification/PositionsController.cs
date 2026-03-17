using Imate.API.Business.Helper;
using Imate.API.Business.Interfaces;
using Imate.API.Business.Interfaces.Classification;
using Imate.API.Common.Router;
using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Imate.API.Presentation.RequestModels.Classification;
using Imate.API.Presentation.ResponseModels.Classification;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Imate.API.Presentation.Controllers.Classification
{
    [Route("api")]
    [ApiController]
    public class PositionsController : ControllerBase
    {
        private readonly IPositionService _positionService;
        private readonly IAuditLogService _auditLogService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ImateDbContext _context;

        public PositionsController(IPositionService positionService, IAuditLogService auditLogService, IUnitOfWork unitOfWork, ImateDbContext context)
        {
            _positionService = positionService;
            _auditLogService = auditLogService;
            _unitOfWork = unitOfWork;
            _context = context;
        }

        private int? GetCurrentUserId()
        {
            if (!User.Identity.IsAuthenticated) return null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId) ? userId : null;
        }
        [HttpGet(APIConfig.Position.GetAllPositions)]
        public async Task<IActionResult> GetAllPositions([FromQuery] CommonParams positionParams)
        {
            var pagedResult = await _positionService.GetAllPositionsAsync(positionParams);
            // Thêm thông tin phân trang vào Response Header để client sử dụng
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
        [HttpPost("positions")]
        public async Task<IActionResult> AddPositions([FromBody] PositionCreateRequest request)
        {
            bool isNameExists = await _context.Positions.AnyAsync(s => s.Name == request.Name);
            if (isNameExists)
            {
                return BadRequest(new { Message = "Tên vị trí đã tồn tại" });
            }
            var positions = await _positionService.AddPositionAsync(request);

            // Create audit log
            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                await _auditLogService.CreateAuditLogAsync(
                    userId.Value,
                    AuditAction.Create,
                    "Position",
                    positions.Id,
                    null
                );
            }

            return Ok(new
            {
                Message = "Thêm mới vị trí thành công",
            });
        }
        [HttpPut("positions/{id}")]
        public async Task<IActionResult> UpdatePosition(int id, [FromBody] PositionUpdateRequest request)
        {
            // Get old values before update
            var existingPosition = await _unitOfWork.Positions.GetPositionByIdAsync(id);
            if (existingPosition == null)
            {
                return NotFound(new { Message = "Không tìm thấy vị trí" });
            }

            bool isNameExists = await _context.Positions.AnyAsync(s => s.Name == request.Name && s.Id != id);

            if (isNameExists)
            {
                return BadRequest(new { Message = "Tên vị trí đã tồn tại" });
            }

            var oldValue = new { existingPosition.Name, existingPosition.IsActive};

            var updatedPosition = await _positionService.UpdatePositionAsync(id, request);

            // Get new value from entity after update (not from request)
            // updatedPosition already has updated PositionSkills collection
            var newValue = new { updatedPosition.Name, updatedPosition.IsActive};

            // Create audit log
            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                await _auditLogService.CreateAuditLogAsync(
                    userId.Value,
                    AuditAction.Update,
                    "Position",
                    id,
                    oldValue,
                    newValue
                );
            }

            return Ok(new
            {
                Message = "Cập nhật vị trí thành công",
            });
        }
        [HttpGet("positions/{positionId}/affected-questions")]
        public async Task<IActionResult> GetAffectedQuestions(int positionId, [FromQuery] bool willBeActive)
        {
            var questions = await _positionService.GetAffectedQuestionsAsync(positionId, willBeActive);
            return Ok(questions);
        }

        [HttpPut("positions/{id}/active")]
        public async Task<IActionResult> ActivatePosition(int id)
        {
            // Get old value
            var existingPosition = await _unitOfWork.Positions.GetPositionByIdAsync(id);
            if (existingPosition == null)
            {
                return NotFound();
            }

            var oldIsActive = existingPosition.IsActive;
            var updatedPosition = await _positionService.SetPositionStatusAsync(id, true);

            if (updatedPosition == null)
            {
                return NotFound();
            }

            // Create audit log
            var userId = GetCurrentUserId();
            if (userId.HasValue && oldIsActive != true)
            {
                await _auditLogService.CreateAuditLogAsync(
                    userId.Value,
                    AuditAction.Update,
                    "Position",
                    id,
                    new { IsActive = oldIsActive },
                    new { IsActive = true }
                );
            }

            return Ok(new
            {
                Message = "Kích hoạt vị trí thành công",
                updatedPosition
            });
        }

        [HttpPut("positions/{id}/inactive")]
        public async Task<IActionResult> DeactivatePosition(int id)
        {
            // Get old value
            var existingPosition = await _unitOfWork.Positions.GetPositionByIdAsync(id);
            if (existingPosition == null)
            {
                return NotFound();
            }

            var oldIsActive = existingPosition.IsActive;
            var updatedPosition = await _positionService.SetPositionStatusAsync(id, false);

            if (updatedPosition == null)
            {
                return NotFound();
            }

            // Create audit log
            var userId = GetCurrentUserId();
            if (userId.HasValue && oldIsActive != false)
            {
                await _auditLogService.CreateAuditLogAsync(
                    userId.Value,
                    AuditAction.Update,
                    "Position",
                    id,
                    new { IsActive = oldIsActive },
                    new { IsActive = false }
                );
            }

            return Ok(new
            {
                Message = "Vô hiệu hóa vị trí thành công",
                updatedPosition
            });
        }
    }
}
