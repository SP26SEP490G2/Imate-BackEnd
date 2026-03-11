using Microsoft.AspNetCore.Mvc;
using Imate.API.Business.Interfaces;
using Imate.API.Presentation.RequestModels.AuditLog;

namespace Imate.API.Presentation.Controllers
{
    [Route("api")]
    [ApiController]
    public class AuditLogController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;

        public AuditLogController(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        [HttpGet("audit-logs")]
        public async Task<IActionResult> GetAuditLogs([FromQuery] AuditLogParams auditLogParams)
        {
            var pagedResult = await _auditLogService.GetAuditLogsAsync(auditLogParams);

            Response.Headers.Add("X-Pagination",
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    pagedResult.TotalCount,
                    pagedResult.PageSize,
                    pagedResult.PageNumber,
                    pagedResult.TotalPages
                }));

            return Ok(pagedResult);
        }

        [HttpGet("audit-logs/{id}")]
        public async Task<IActionResult> GetAuditLogDetail(int id)
        {
            var auditLog = await _auditLogService.GetAuditLogDetailAsync(id);
            return Ok(auditLog);
        }

        [HttpGet("audit-logs/filter-options")]
        public async Task<IActionResult> GetFilterOptions()
        {
            var options = await _auditLogService.GetFilterOptionsAsync();
            return Ok(options);
        }
    }
}

