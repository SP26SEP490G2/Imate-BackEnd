using Imate.API.Business.Interfaces.Mentors;
using Imate.API.Presentation.RequestModels.Mentors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Imate.API.Presentation.Controllers.Mentors
{
    [Route("api/bookings")]
    [ApiController]
    public class MentorsBookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public MentorsBookingController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpPost]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> CreateBooking([FromBody] BookingCreateRequest request)
        {
            var candidateId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _bookingService.CreateBookingAsync(request, candidateId);
            return Ok(result);
        }

        [HttpGet("mentor/{mentorId}")]
        public async Task<IActionResult> GetBookedSlotsByMentorId(int mentorId)
        {
            var result = await _bookingService.GetBookedSlotsByMentorIdAsync(mentorId);
            return Ok(result);
        }

        [HttpGet("mentor/my-bookings")]
        [Authorize(Roles = "Mentor")]
        public async Task<IActionResult> GetMyBookings()
        {
            var mentorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _bookingService.GetMentorBookingsAsync(mentorId);
            return Ok(result);
        }

        [HttpGet("mentor/history-session")]
        [Authorize(Roles = "Mentor")]
        public async Task<IActionResult> GetMentorHistorySession()
        {
            var mentorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _bookingService.GetMentorCompletedBookingsSummaryAsync(mentorId);
            return Ok(result);
        }

        [HttpGet("mentor/history-session/{sessionId}")]
        [Authorize(Roles = "Mentor")]
        public async Task<IActionResult> GetMentorHistorySessionDetail(int sessionId)
        {
            var mentorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _bookingService.GetMentorSessionDetailAsync(mentorId, sessionId);
            return Ok(result);
        }

        [HttpGet("candidate/history-session")]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> GetCandidateHistorySession()
        {
            var candidateId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _bookingService.GetCandidateCompletedBookingsSummaryAsync(candidateId);
            return Ok(result);
        }

        [HttpGet("candidate/history-session/{sessionId}")]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> GetCandidateHistorySessionDetail(int sessionId)
        {
            var candidateId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _bookingService.GetCandidateSessionDetailAsync(candidateId, sessionId);
            return Ok(result);
        }

        [HttpPut("{bookingId}/cancel")]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            var candidateId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _bookingService.CancelBookingAsync(bookingId, candidateId);
            return Ok(new { message = "Booking cancelled successfully." });
        }

        [HttpPost("{bookingId}/rate")]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> RateMentor(int bookingId, [FromBody] RateMentorRequest request)
        {
            var candidateId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _bookingService.RateMentorAsync(bookingId, candidateId, request);
            return Ok(new { message = "Mentor rated successfully." });
        }
    }
}
