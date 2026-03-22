using Imate.API.Presentation.RequestModels.Mentors;
using Imate.API.Presentation.ResponseModels.Mentors;

namespace Imate.API.Business.Interfaces.Mentors
{
    public interface IBookingService
    {
        Task<BookingResponseModel> CreateBookingAsync(BookingCreateRequest request, int candidateId);
        Task<List<MentorBookedSlotResponse>> GetBookedSlotsByMentorIdAsync(int mentorId);
        Task<List<BookingDetailResponse>> GetCandidateBookingsAsync(int candidateId);
        Task<List<BookingDetailResponse>> GetMentorBookingsAsync(int mentorId);
        Task CancelBookingAsync(int bookingId, int candidateId);
        Task RateMentorAsync(int bookingId, int candidateId, RateMentorRequest request);
    }
}
