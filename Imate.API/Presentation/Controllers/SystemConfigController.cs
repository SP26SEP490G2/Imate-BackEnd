using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imate.API.Business.Interfaces;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Models.Enums;
using Imate.API.Presentation.RequestModels;
using System.Security.Claims;

namespace Imate.API.Presentation.Controllers
{
    [Route("api/system-config")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class SystemConfigController : ControllerBase
    {
        private readonly ISystemConfigService _systemConfigService;
        private readonly IAuditLogService _auditLogService;
        private readonly IUnitOfWork _unitOfWork;

        public SystemConfigController(ISystemConfigService systemConfigService, IAuditLogService auditLogService, IUnitOfWork unitOfWork)
        {
            _systemConfigService = systemConfigService;
            _auditLogService = auditLogService;
            _unitOfWork = unitOfWork;
        }

        private int? GetCurrentUserId()
        {
            if (!User.Identity.IsAuthenticated) return null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId) ? userId : null;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllConfigs()
        {
            var configs = await _systemConfigService.GetAllConfigsAsync();
            return Ok(new { data = configs, message = "Lấy danh sách cấu hình thành công" });
        }

        [HttpGet("{key}")]
        public async Task<IActionResult> GetConfigByKey(string key)
        {
            var config = await _systemConfigService.GetConfigByKeyAsync(key);
            if (config == null)
            {
                return NotFound(new { message = $"Không tìm thấy cấu hình với key: {key}" });
            }
            return Ok(new { data = config, message = "Lấy cấu hình thành công" });
        }

        [HttpPut("{key}")]
        public async Task<IActionResult> UpdateConfig(string key, [FromBody] UpdateSystemConfigRequest request)
        {
            try
            {
                // Get old value before update
                var existingConfig = await _unitOfWork.SystemConfigs.GetByKeyAsync(key);
                if (existingConfig == null)
                {
                    return NotFound(new { message = $"Không tìm thấy cấu hình với key: {key}" });
                }

                var oldValue = new { Key = existingConfig.Key, Value = existingConfig.Value, Description = existingConfig.Description };

                // Update config
                var updatedConfig = await _systemConfigService.UpdateConfigAsync(key, request.Value);

                // Get new value from entity after update
                var newValue = new { Key = updatedConfig.Key, Value = updatedConfig.Value, Description = updatedConfig.Description };

                // Create audit log
                var userId = GetCurrentUserId();
                if (userId.HasValue)
                {
                    await _auditLogService.CreateAuditLogAsync(
                        userId.Value,
                        AuditAction.Update,
                        "SystemConfig",
                        existingConfig.Id,
                        oldValue,
                        newValue
                    );
                }

                return Ok(new { data = updatedConfig, message = "Cập nhật cấu hình thành công" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

