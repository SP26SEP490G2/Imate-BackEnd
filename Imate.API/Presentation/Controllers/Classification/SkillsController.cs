using Imate.API.Business.Helper;
using Imate.API.Business.Interfaces;
using Imate.API.Business.Interfaces.Classification;
using Imate.API.Common.Router;
using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Models.Enums;
using Imate.API.Presentation.RequestModels.Classification;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Imate.API.Presentation.Controllers.Classification
{
    [Route("api")]
    [ApiController]
    public class SkillsController : ControllerBase
    {
        private readonly ISkillService _skillService;
        private readonly IAuditLogService _auditLogService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ImateDbContext _context;

        public SkillsController(ISkillService skillService, IAuditLogService auditLogService, IUnitOfWork unitOfWork, ImateDbContext context)
        {
            _skillService = skillService;
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
        [HttpGet(APIConfig.Skills.GetAllSkills)]
        public async Task<IActionResult> GetAllSkills([FromQuery] CommonParams skillParams)
        {
            var pagedResult = await _skillService.GetAllSkillsAsync(skillParams);
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
        [HttpPut("skills/{skillId}")]
        public async Task<IActionResult> UpdateSkills(int skillId, [FromBody] SkillUpdateRequest skill)
        {
            // Get old values before update
            var existingSkill = await _unitOfWork.Skills.GetSkillByIdAsync(skillId);
            if (existingSkill == null)
            {
                return NotFound(new { Message = "Không tìm thấy kĩ năng" });
            }

            bool isNameExists = await _context.Skills.AnyAsync(s => s.Name == skill.Name && s.Id != skillId);

            if (isNameExists)
            {
                return BadRequest(new { Message = "Tên kĩ năng đã tồn tại" });
            }

            var oldValue = new { existingSkill.Name, existingSkill.IsActive };
            var updatedSkill = await _skillService.UpdateSkillsAsync(skillId, skill);

            // Get new value from entity after update (not from request)
            var newValue = new { updatedSkill.Name, updatedSkill.IsActive };

            // Create audit log
            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                await _auditLogService.CreateAuditLogAsync(
                    userId.Value,
                    AuditAction.Update,
                    "Skill",
                    skillId,
                    oldValue,
                    newValue
                );
            }

            return Ok(new
            {
                Message = "Cập nhật kĩ năng thành công",
                skill
            });
        }
        [HttpPost("skills")]
        public async Task<IActionResult> AddSkills([FromBody] SkillCreateRequest skill)
        {
            bool isNameExists = await _context.Skills.AnyAsync(s => s.Name == skill.Name);
            if (isNameExists)
            {
                return BadRequest(new { Message = "Tên kĩ năng đã tồn tại" });
            }
            var newSkill = await _skillService.AddSkillsAsync(skill);

            // Create audit log
            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                await _auditLogService.CreateAuditLogAsync(
                    userId.Value,
                    AuditAction.Create,
                    "Skill",
                    newSkill.Id,
                    null,
                    new { newSkill.Name, newSkill.IsActive }
                );
            }

            return Ok(new
            {
                Message = "Thêm mới kỹ năng thành công",
                newSkill

            });
        }
        [HttpGet("skills/{skillId}/affected-questions")]
        public async Task<IActionResult> GetAffectedQuestions(int skillId, [FromQuery] bool willBeActive)
        {
            var questions = await _skillService.GetAffectedQuestionsAsync(skillId, willBeActive);
            return Ok(questions);
        }

        [HttpPut("skills/{id}/active")]
        public async Task<IActionResult> ActivateSkill(int id)
        {
            // Get old value
            var existingSkill = await _unitOfWork.Skills.GetSkillByIdAsync(id);
            if (existingSkill == null)
            {
                return NotFound();
            }

            var oldIsActive = existingSkill.IsActive;
            var updatedSkill = await _skillService.SetSkillStatusAsync(id, true);

            if (updatedSkill == null)
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
                    "Skill",
                    id,
                    new { IsActive = oldIsActive },
                    new { IsActive = true }
                );
            }

            return Ok(new
            {
                Message = "Kích hoạt kỹ năng thành công",
                updatedSkill
            });
        }

        [HttpPut("skills/{id}/inactive")]
        public async Task<IActionResult> DeactivateSkill(int id)
        {
            // Get old value
            var existingSkill = await _unitOfWork.Skills.GetSkillByIdAsync(id);
            if (existingSkill == null)
            {
                return NotFound();
            }

            var oldIsActive = existingSkill.IsActive;
            var updatedSkill = await _skillService.SetSkillStatusAsync(id, false);

            if (updatedSkill == null)
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
                    "Skill",
                    id,
                    new { IsActive = oldIsActive },
                    new { IsActive = false }
                );
            }

            return Ok(new
            {
                Message = "Vô hiệu hóa kỹ năng thành công",
                updatedSkill
            });
        }
    }
}
