using Imate.API.Business.Interfaces.Mentors;
using Imate.API.Presentation.RequestModels.Mentors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Imate.API.Presentation.Controllers.Mentors
{
    [Route("api/bookings")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingController(IBookingService bookingService)
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
    }
}
