using Microsoft.AspNetCore.Mvc;
using Imate.API.Business.Interfaces;
using Imate.API.Business.Interfaces.Classification;
using Imate.API.Business.Interfaces.QuestionBank;
using Imate.API.DataAccess.Interfaces.Classification;
using Imate.API.Models.Enums;
using Imate.API.Presentation.RequestModels.Classification;
using Imate.API.Presentation.ResponseModels.Classification;
using System.Security.Claims;
using Imate.API.Common.Router;

namespace Imate.API.Presentation.Controllers.Classification
{
    [Route("api")]
    [ApiController]
    public class CompanyController : ControllerBase
    {
        private readonly ICompanyService _companyService;
        private readonly IAuditLogService _auditLogService;
        private readonly ICompanyRepository _companyRepository;
        private readonly IQuestionService _questionService;

        public CompanyController(
            ICompanyService companyService, 
            IAuditLogService auditLogService, 
            ICompanyRepository companyRepository,
            IQuestionService questionService)
        {
            _companyService = companyService;
            _auditLogService = auditLogService;
            _companyRepository = companyRepository;
            _questionService = questionService;
        }

        private int? GetCurrentUserId()
        {
            if (!User.Identity.IsAuthenticated) return null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId) ? userId : null;
        }

        [HttpGet(APIConfig.Companies.GetAllCompanies)]
        public async Task<IActionResult> GetCompanyList([FromQuery] CompanyListRequestModel request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var list = await _companyService.GetCompanyListAsync(request);

            return Ok(list);
        }


        [HttpPost("staff-create-company")]
        public async Task<IActionResult> CreateCompany([FromForm] CreateCompanyRequestModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var newCompany = await _companyService.CreateCompanyAsync(model);
                
                // Create audit log
                var userId = GetCurrentUserId();
                if (userId.HasValue)
                {
                    await _auditLogService.CreateAuditLogAsync(
                        userId.Value,
                        AuditAction.Create,
                        "Company",
                        newCompany.Id,
                        null,
                        new { newCompany.Name, newCompany.IsActive }
                    );
                }

                return CreatedAtAction(nameof(GetCompany), new { id = newCompany.Id }, newCompany);
            }
            catch (InvalidOperationException ex)
            {
               
                return Conflict(new { message = ex.Message });
            }
        }

        
        [HttpGet("company-staff/{id}")]
        public async Task<IActionResult> GetCompany(int id)
        {
            var company = await _companyService.GetCompanyDetailsAsync(id);

            if (company == null)
            {
                return NotFound(); 
            }

            return Ok(company); 
        }

       
        [HttpPut("company-staff/{id}")]
        public async Task<IActionResult> UpdateCompany(int id, [FromForm] UpdateCompanyRequestModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Get old values before update
                var existingCompany = await _companyRepository.GetByIdAsync(id);
                if (existingCompany == null)
                {
                    return NotFound(new { Message = "Company not found" });
                }

                var oldValue = new { existingCompany.Name, existingCompany.IsActive };

                var updatedCompany = await _companyService.UpdateCompanyAsync(id, model);

                if (updatedCompany == null)
                {
                    return NotFound();
                }

                var newValue = new { updatedCompany.Name, updatedCompany.IsActive };

                // Create audit log
                var userId = GetCurrentUserId();
                if (userId.HasValue)
                {
                    await _auditLogService.CreateAuditLogAsync(
                        userId.Value,
                        AuditAction.Update,
                        "Company",
                        id,
                        oldValue,
                        newValue
                    );
                }

                return Ok(updatedCompany);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }


        [HttpPut("company-staff/{id}/active")]
        public async Task<IActionResult> ActivateCompany(int id)
        {
            // Get old value
            var existingCompany = await _companyRepository.GetByIdAsync(id);
            if (existingCompany == null)
            {
                return NotFound();
            }

            var oldIsActive = existingCompany.IsActive;
            var updatedCompany = await _companyService.SetCompanyStatusAsync(id, true);

            if (updatedCompany == null)
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
                    "Company",
                    id,
                    new { IsActive = oldIsActive },
                    new { IsActive = true }
                );
            }

            return Ok(updatedCompany); 
        }

        [HttpPut("company-staff/{id}/inactive")]
        public async Task<IActionResult> DeactivateCompany(int id)
        {
            // Get old value
            var existingCompany = await _companyRepository.GetByIdAsync(id);
            if (existingCompany == null)
            {
                return NotFound();
            }

            var oldIsActive = existingCompany.IsActive;
            var updatedCompany = await _companyService.SetCompanyStatusAsync(id, false);

            if (updatedCompany == null)
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
                    "Company",
                    id,
                    new { IsActive = oldIsActive },
                    new { IsActive = false }
                );
            }

            return Ok(updatedCompany); 
        }

        /// <summary>
        /// Get positions and skills from questions related to a company
        /// Used for manual interview setup - filters positions/skills based on available questions
        /// </summary>
        [HttpGet("company/{companyId}/positions-skills")]
        public async Task<IActionResult> GetPositionsAndSkillsByCompany(int companyId)
        {
            try
            {
                // Verify company exists
                var company = await _companyRepository.GetByIdAsync(companyId);
                if (company == null)
                {
                    return NotFound(new { message = $"Company with ID {companyId} not found." });
                }

                var result = await _questionService.GetPositionsAndSkillsByCompanyAsync(companyId);
                
                return Ok(new
                {
                    success = true,
                    data = result,
                    message = "Lấy danh sách positions và skills thành công"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi lấy danh sách positions và skills",
                    error = ex.Message
                });
            }
        }
    }
}
