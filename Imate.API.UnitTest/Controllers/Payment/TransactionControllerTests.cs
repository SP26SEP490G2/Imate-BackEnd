using Moq;
using FluentAssertions;
using Imate.API.Presentation.Controllers.Payment;
using Imate.API.Business.Interfaces.Payment;
using Imate.API.Presentation.RequestModels.Payment;
using Imate.API.Presentation.ResponseModels.Payment;
using Imate.API.Business.Helper;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Imate.API.UnitTest.Controllers.Payment
{
    public class TransactionControllerTests
    {
        private readonly Mock<ITransactionService> _mockTransactionService;
        private readonly TransactionController _controller;

        public TransactionControllerTests()
        {
            _mockTransactionService = new Mock<ITransactionService>();
            _controller = new TransactionController(_mockTransactionService.Object);
        }

        private void SetupUser(int userId, string role = "Candidate")
        {
            var claims = new List<Claim> 
            { 
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext { User = principal };
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        }

        [Fact]
        public async Task GetAllTransactions_Success_ForStaff()
        {
            // Arrange
            SetupUser(1, "Staff");
            var queryParams = new TransactionQueryParameters();
            var mockResults = new List<TransactionResponse> { new TransactionResponse { TransactionId = 1 } };
            var pagedResult = new PagedList<TransactionResponse>(mockResults, 1, 1, 10);
            
            _mockTransactionService.Setup(s => s.GetAllTransactionsForAdminAsync(queryParams))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllTransactions(queryParams);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(pagedResult);
        }

        [Fact]
        public async Task ApproveWithdrawal_Success()
        {
            // Arrange
            SetupUser(1, "Staff");
            var transactionId = 100;
            var request = new WithdrawalActionRequest { ResponseNote = "Approved" };

            // Act
            var result = await _controller.ApproveWithdrawal(transactionId, request);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _mockTransactionService.Verify(s => s.ApproveWithdrawalAsync(transactionId, 1, "Approved"), Times.Once);
        }

        [Fact]
        public async Task RejectWithdrawal_Success()
        {
            // Arrange
            SetupUser(1, "Staff");
            var transactionId = 100;
            var request = new WithdrawalActionRequest { ResponseNote = "Rejected" };

            // Act
            var result = await _controller.RejectWithdrawal(transactionId, request);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _mockTransactionService.Verify(s => s.RejectWithdrawalAsync(transactionId, 1, "Rejected"), Times.Once);
        }

        [Fact]
        public async Task GetReadyForPayoutBookings_Success()
        {
            // Arrange
            SetupUser(1, "Staff");
            var queryParams = new TransactionQueryParameters();
            var mockResults = new List<TransactionResponse>();
            var pagedResult = new PagedList<TransactionResponse>(mockResults, 0, 1, 10);

            _mockTransactionService.Setup(s => s.GetReadyForPayoutBookingsAsync(queryParams))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetReadyForPayoutBookings(queryParams);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        }

        [Fact]
        public async Task ProcessBookingPayout_Success()
        {
            // Arrange
            SetupUser(1, "Staff");
            var transactionId = 100;
            var request = new WithdrawalActionRequest { ResponseNote = "Payout processed" };

            // Act
            var result = await _controller.ProcessBookingPayout(transactionId, request);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _mockTransactionService.Verify(s => s.ProcessBookingPayoutAsync(transactionId, 1, "Payout processed"), Times.Once);
        }

        [Fact]
        public async Task ApproveWithdrawal_NotFound_WhenIdInvalid()
        {
            // Arrange
            SetupUser(1, "Staff");
            var transactionId = 999;
            var request = new WithdrawalActionRequest { ResponseNote = "Approved" };
            _mockTransactionService.Setup(s => s.ApproveWithdrawalAsync(transactionId, 1, "Approved"))
                .ThrowsAsync(new KeyNotFoundException("Transaction not found"));

            // Act
            var result = await _controller.ApproveWithdrawal(transactionId, request);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task ApproveWithdrawal_Forbidden_WhenUserIsNotStaff()
        {
            // Arrange
            SetupUser(1, "Candidate"); // Non-staff
            var transactionId = 100;
            var request = new WithdrawalActionRequest { ResponseNote = "Approved" };

            // Act
            var result = await _controller.ApproveWithdrawal(transactionId, request);

            // Assert
            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task RejectWithdrawal_BadRequest_WhenNoteIsEmpty()
        {
            // Arrange
            SetupUser(1, "Staff");
            var transactionId = 100;
            var request = new WithdrawalActionRequest { ResponseNote = "" };
            
            _mockTransactionService.Setup(s => s.RejectWithdrawalAsync(transactionId, 1, ""))
                .ThrowsAsync(new ArgumentException("Note required"));

            // Act
            var result = await _controller.RejectWithdrawal(transactionId, request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task RejectWithdrawal_NotFound_WhenIdInvalid()
        {
            // Arrange
            SetupUser(1, "Staff");
            var transactionId = 999;
            var request = new WithdrawalActionRequest { ResponseNote = "Rejected" };
            _mockTransactionService.Setup(s => s.RejectWithdrawalAsync(transactionId, 1, "Rejected"))
                .ThrowsAsync(new KeyNotFoundException("Transaction not found"));

            // Act
            var result = await _controller.RejectWithdrawal(transactionId, request);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task ApproveWithdrawal_BadRequest_WhenAlreadyProcessed()
        {
            // Arrange
            SetupUser(1, "Staff");
            var transactionId = 100;
            var request = new WithdrawalActionRequest { ResponseNote = "Approved" };
            _mockTransactionService.Setup(s => s.ApproveWithdrawalAsync(transactionId, 1, "Approved"))
                .ThrowsAsync(new ArgumentException("Transaction already processed"));

            // Act
            var result = await _controller.ApproveWithdrawal(transactionId, request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task RejectWithdrawal_BadRequest_WhenAlreadyProcessed()
        {
            // Arrange
            SetupUser(1, "Staff");
            var transactionId = 100;
            var request = new WithdrawalActionRequest { ResponseNote = "Rejected" };
            _mockTransactionService.Setup(s => s.RejectWithdrawalAsync(transactionId, 1, "Rejected"))
                .ThrowsAsync(new ArgumentException("Transaction already rejected"));

            // Act
            var result = await _controller.RejectWithdrawal(transactionId, request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task ProcessBookingPayout_Forbidden_WhenUserIsCandidate()
        {
            // Arrange
            SetupUser(1, "Candidate");
            var transactionId = 100;
            var request = new WithdrawalActionRequest { ResponseNote = "Payout" };

            // Act
            var result = await _controller.ProcessBookingPayout(transactionId, request);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task ApproveWithdrawal_VerifiesServiceCalledWithCorrectParams()
        {
            // Arrange
            SetupUser(5, "Staff");
            var transactionId = 200;
            var request = new WithdrawalActionRequest { ResponseNote = "Audit note" };

            // Act
            var result = await _controller.ApproveWithdrawal(transactionId, request);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _mockTransactionService.Verify(
                s => s.ApproveWithdrawalAsync(200, 5, "Audit note"),
                Times.Once);
        }

        [Fact]
        public async Task RejectWithdrawal_VerifiesServiceCalledWithCorrectParams()
        {
            // Arrange
            SetupUser(5, "Staff");
            var transactionId = 200;
            var request = new WithdrawalActionRequest { ResponseNote = "Reject audit" };

            // Act
            var result = await _controller.RejectWithdrawal(transactionId, request);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _mockTransactionService.Verify(
                s => s.RejectWithdrawalAsync(200, 5, "Reject audit"),
                Times.Once);
        }
    }
}
