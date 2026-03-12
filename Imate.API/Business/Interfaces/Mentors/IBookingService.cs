using Imate.API.Presentation.RequestModels.Mentors;
using Imate.API.Presentation.ResponseModels.Mentors;

namespace Imate.API.Business.Interfaces.Mentors
{
    public interface IBookingService
    {
        Task<BookingResponseModel> CreateBookingAsync(BookingCreateRequest request, int candidateId);
    }
}
