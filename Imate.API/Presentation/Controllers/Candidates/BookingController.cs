using Imate.API.Business.Interfaces.Mentors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Imate.API.Presentation.Controllers.Candidates
{
    [Route("api/candidates/bookings")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpGet]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> GetMyBookings()
        {
            var candidateId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _bookingService.GetCandidateBookingsAsync(candidateId);
            return Ok(result);
        }
    }
}
