using DocumentFormat.OpenXml.Spreadsheet;
using Imate.API.Business.Helper;
using Imate.API.Business.Interfaces.Payment;
using Imate.API.Business.Services.Payment;
using Imate.API.Presentation.RequestModels.Payment;
using Imate.API.Presentation.ResponseModels.Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using static Google.Apis.Requests.BatchRequest;
using SystemStatisticsResponse = Imate.API.Presentation.ResponseModels.Payment.SystemStatisticsResponse;

namespace Imate.API.Presentation.Controllers.Payment
{
    [ApiController]
    [Route("api")]
    [Authorize]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _service;
        public TransactionController(ITransactionService service)
        {
            _service = service;
        }

        [HttpGet("transactions/recent")]
        public async Task<ActionResult<List<TransactionResponse>>> GetRecentTransactions([FromQuery] int take = 5)
        {
            var accountId = GetCurrentAccountId();
            if (accountId == null)
            {
                return Unauthorized("Không thể xác định tài khoản.");
            }

            var transactions = await _service
                .GetRecentTransactionsAsync(accountId.Value, take);

            return Ok(transactions);
        }

        [HttpGet("transactions")] // Đặt route tại /api/transactions
        public async Task<ActionResult<PagedList<TransactionResponse>>> GetTransactions(
        [FromQuery] TransactionQueryParameters paginationParams)
        {
            var accountId = GetCurrentAccountId();
            if (accountId == null)
            {
                return Unauthorized("Không thể xác định tài khoản.");
            }

            try
            {
                // Service mới, ta sẽ tạo ở bước sau
                var pagedTransactions = await _service
                    .GetTransactionsAsync(accountId.Value, paginationParams);

                // Trả về PagedList trực tiếp, ASP.NET Core sẽ serialize nó
                return Ok(pagedTransactions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Lỗi hệ thống: " + ex.Message);
            }
        }
        [HttpGet("balance")]
        public async Task<ActionResult<BalanceSummaryResponse>> GetBalanceSummary()
        {
            var accountId = GetCurrentAccountId();

            if (accountId == null)
            {
                return Unauthorized("Không thể xác định tài khoản.");
            }

            try
            {
                var summaryDto = await _service.GetBalanceSummaryAsync(accountId.Value);
                return Ok(summaryDto);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Lỗi hệ thống: " + ex.Message);
            }
        }

        [HttpPost("deposit")]
        public async Task<ActionResult<TransactionResponse>> CreateDeposit([FromBody] DepositRequest depositRequest)
        {
            var accountId = GetCurrentAccountId();
            if (accountId == null)
            {
                return Unauthorized("Không thể xác định tài khoản.");
            }

            try
            {
                var response = await _service
                    .CreateDepositAsync(accountId.Value, depositRequest);

                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CreateDeposit Controller Error: {ex.Message}");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("withdraw")]
        public async Task<ActionResult<TransactionResponse>> CreateWithdrawal(
        [FromBody] WithdrawRequest withdrawRequestDto)
        {
            var accountId = GetCurrentAccountId();
            var role = GetCurrentUserRole();

            if (accountId == null || string.IsNullOrEmpty(role))
            {
                return Unauthorized("Không thể xác định tài khoản hoặc vai trò của người dùng.");
            }

            try
            {
                // Truyền `role` xuống service
                var transactionDto = await _service
                    .CreateWithdrawalAsync(accountId.Value, role, withdrawRequestDto);

                return CreatedAtAction(
                    nameof(GetTransactions),
                    new { id = transactionDto.TransactionId },
                    transactionDto
                );
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex) // Bắt lỗi nghiệp vụ (ví dụ: "Số dư không đủ", "Thiếu thông tin ngân hàng")
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
            }
        }

        [HttpPut("transactions/{transactionId}/cancel")]
        public async Task<IActionResult> CancelTransaction(int transactionId)
        {
            var accountId = GetCurrentAccountId();
            if (accountId == null)
            {
                return Unauthorized("Không thể xác định tài khoản.");
            }

            try
            {
                await _service.CancelTransactionAsync(transactionId, accountId.Value);
                return Ok(new { message = "Giao dịch đã được hủy thành công" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
            }
        }

        // ========== ADMIN/STAFF ENDPOINTS ==========

        [HttpGet("admin/transactions")]
        public async Task<ActionResult<PagedList<TransactionResponse>>> GetAllTransactions(
            [FromQuery] TransactionQueryParameters paginationParams)
        {
            var role = GetCurrentUserRole();
            if (role != "Admin" && role != "Staff")
            {
                return Forbid("Chỉ admin và staff mới có quyền truy cập.");
            }

            try
            {
                var pagedTransactions = await _service.GetAllTransactionsForAdminAsync(paginationParams);
                return Ok(pagedTransactions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
            }
        }

        [HttpPut("admin/transactions/{transactionId}/approve")]
        public async Task<IActionResult> ApproveWithdrawal(int transactionId, [FromBody] WithdrawalActionRequest? request = null)
        {
            var reviewerId = GetCurrentAccountId();
            var role = GetCurrentUserRole();

            if (reviewerId == null)
            {
                return Unauthorized("Không thể xác định tài khoản.");
            }

            if (role != "Admin" && role != "Staff")
            {
                return StatusCode(403, "Chỉ admin và staff mới có quyền duyệt yêu cầu rút tiền.");
            }

            try
            {
                await _service.ApproveWithdrawalAsync(transactionId, reviewerId.Value, request?.ResponseNote);
                return Ok(new { message = "Yêu cầu rút tiền đã được duyệt thành công." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
            }
        }

        [HttpPut("admin/transactions/{transactionId}/reject")]
        public async Task<IActionResult> RejectWithdrawal(int transactionId, [FromBody] WithdrawalActionRequest? request = null)
        {
            var reviewerId = GetCurrentAccountId();
            var role = GetCurrentUserRole();

            if (reviewerId == null)
            {
                return Unauthorized("Không thể xác định tài khoản.");
            }

            if (role != "Admin" && role != "Staff")
            {
                return Forbid("Chỉ admin và staff mới có quyền từ chối yêu cầu rút tiền.");
            }

            try
            {
                await _service.RejectWithdrawalAsync(transactionId, reviewerId.Value, request?.ResponseNote);
                return Ok(new { message = "Yêu cầu rút tiền đã bị từ chối." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
            }
        }

        [HttpGet("admin/transactions/statistics")]
        public async Task<ActionResult<SystemStatisticsResponse>> GetSystemStatistics()
        {
            var role = GetCurrentUserRole();
            if (role != "Admin" && role != "Staff")
            {
                return Forbid("Chỉ admin và staff mới có quyền truy cập.");
            }

            try
            {
                var statistics = await _service.GetSystemStatisticsAsync();
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
            }
        }

        [HttpGet("admin/transactions/ready-for-payout")]
        public async Task<ActionResult<PagedList<TransactionResponse>>> GetReadyForPayoutBookings([FromQuery] TransactionQueryParameters paginationParams)
        {
            var role = GetCurrentUserRole();
            if (role != "Admin" && role != "Staff")
            {
                return Forbid("Chỉ admin và staff mới có quyền truy cập.");
            }

            try
            {
                var transactions = await _service.GetReadyForPayoutBookingsAsync(paginationParams);
                return Ok(transactions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
            }
        }

        [HttpPut("admin/transactions/{transactionId}/process-payout")]
        public async Task<IActionResult> ProcessBookingPayout(int transactionId, [FromBody] WithdrawalActionRequest? request = null)
        {
            var reviewerId = GetCurrentAccountId();
            var role = GetCurrentUserRole();

            if (reviewerId == null)
            {
                return Unauthorized("Không thể xác định tài khoản.");
            }

            if (role != "Admin" && role != "Staff")
            {
                return Forbid("Chỉ admin và staff mới có quyền xử lý payout booking.");
            }

            try
            {
                await _service.ProcessBookingPayoutAsync(transactionId, reviewerId.Value, request?.ResponseNote);
                return Ok(new { message = "Payout booking đã được xử lý thành công." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
            }
        }

        [HttpGet("admin/transactions/revenue")]
        public async Task<ActionResult<RevenueResponse>> GetRevenue([FromQuery] RevenueQueryParameters parameters)
        {
            var role = GetCurrentUserRole();
            if (role != "Admin" && role != "Staff")
            {
                return Forbid("Chỉ admin và staff mới có quyền truy cập báo cáo doanh thu.");
            }

            try
            {
                var revenue = await _service.GetRevenueAsync(parameters);
                return Ok(revenue);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
            }
        }

        [HttpGet("admin/transactions/revenue-transactions")]
        public async Task<ActionResult<PagedList<TransactionResponse>>> GetRevenueTransactions([FromQuery] RevenueTransactionQueryParameters parameters)
        {
            var role = GetCurrentUserRole();
            if (role != "Admin" && role != "Staff")
            {
                return Forbid("Chỉ admin và staff mới có quyền truy cập.");
            }
            try
            {
                var transactions = await _service.GetRevenueTransactionsAsync(parameters);
                return Ok(transactions);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi hệ thống: {ex.Message}");
            }
        }

        // Thêm hàm helper để lấy Role
        private string? GetCurrentUserRole()
        {
            return User.FindFirstValue(ClaimTypes.Role);
        }
        private int? GetCurrentAccountId()
        {
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst("accountId")?.Value;

            if (accountIdClaim == null || !int.TryParse(accountIdClaim, out int accountId))
            {
                return null;
            }
            return accountId;
        }
    }
}
