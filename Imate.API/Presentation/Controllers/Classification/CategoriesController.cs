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
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        private readonly IAuditLogService _auditLogService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ImateDbContext _context;

        public CategoriesController(ICategoryService categoryService, IAuditLogService auditLogService, IUnitOfWork unitOfWork, ImateDbContext context)
        {
            _categoryService = categoryService;
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
        [HttpGet("get-categories")]
        public async Task<IActionResult> GetAllCategories([FromQuery] CommonParams categoryParams)
        {
            var pagedResult = await _categoryService.GetAllCategoryAsync(categoryParams);

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
        [HttpPut("categories/{categoryId}")]
        public async Task<IActionResult> UpdateCategories(int categoryId, [FromBody] CategoryUpdateRequest category)
        {
            // Get old values before update
            var existingCategory = await _unitOfWork.Categories.GetCategoryByIdAsync(categoryId);
            if (existingCategory == null)
            {
                return NotFound(new { Message = "Không tìm thấy thể loại" });
            }

            bool isNameExists = await _context.Categories.AnyAsync(c => c.Name == category.Name && c.Id != categoryId);

            if (isNameExists)
            {
                return BadRequest(new { Message = "Tên thể loại đã tồn tại" });
            }

            var oldValue = new { existingCategory.Name, existingCategory.IsActive };
            var updatedCategory = await _categoryService.UpdateCategoriesAsync(categoryId, category);

            // Get new value from entity after update (not from request)
            var newValue = new { updatedCategory.Name, updatedCategory.IsActive };

            // Create audit log
            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                await _auditLogService.CreateAuditLogAsync(
                    userId.Value,
                    AuditAction.Update,
                    "Category",
                    categoryId,
                    oldValue,
                    newValue
                );
            }

            return Ok(new
            {
                Message = "Cập nhật danh mục thành công",
                category
            });
        }
        [HttpPost("categories")]
        public async Task<IActionResult> AddCategories([FromBody] CategoryCreateRequest category)
        {
            bool isNameExists = await _context.Categories.AnyAsync(c => c.Name == category.Name);
            if (isNameExists)
            {
                return BadRequest(new { Message = "Tên thể loại đã tồn tại" });
            }

            var newCategory = await _categoryService.AddCategoriesAsync(category);

            // Create audit log
            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                await _auditLogService.CreateAuditLogAsync(
                    userId.Value,
                    AuditAction.Create,
                    "Category",
                    newCategory.Id,
                    null,
                    new { newCategory.Name, newCategory.IsActive }
                );
            }

            return Ok(new
            {
                Message = "Thêm mới danh mục thành công",
                newCategory
            });
        }
        [HttpGet("categories/{categoryId}/affected-questions")]
        public async Task<IActionResult> GetAffectedQuestions(int categoryId, [FromQuery] bool willBeActive)
        {
            var questions = await _categoryService.GetAffectedQuestionsAsync(categoryId, willBeActive);
            return Ok(questions);
        }

        [HttpPut("categories/{id}/active")]
        public async Task<IActionResult> ActivateCategory(int id)
        {
            // Get old value
            var existingCategory = await _unitOfWork.Categories.GetCategoryByIdAsync(id);
            if (existingCategory == null)
            {
                return NotFound();
            }

            var oldIsActive = existingCategory.IsActive;
            var updatedCategory = await _categoryService.SetCategoryStatusAsync(id, true);

            if (updatedCategory == null)
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
                    "Category",
                    id,
                    new { IsActive = oldIsActive },
                    new { IsActive = true }
                );
            }

            return Ok(new
            {
                Message = "Kích hoạt danh mục thành công",
                updatedCategory
            });
        }

        [HttpPut("categories/{id}/deactive")]
        public async Task<IActionResult> DeactivateCategory(int id)
        {
            // Get old value
            var existingCategory = await _unitOfWork.Categories.GetCategoryByIdAsync(id);
            if (existingCategory == null)
            {
                return NotFound();
            }

            var oldIsActive = existingCategory.IsActive;
            var updatedCategory = await _categoryService.SetCategoryStatusAsync(id, false);

            if (updatedCategory == null)
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
                    "Category",
                    id,
                    new { IsActive = oldIsActive },
                    new { IsActive = false }
                );
            }

            return Ok(new
            {
                Message = "Vô hiệu hóa danh mục thành công",
                updatedCategory
            });
        }
    }
}