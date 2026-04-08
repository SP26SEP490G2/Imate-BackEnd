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
        Task<List<MentorSessionSummaryResponse>> GetMentorCompletedBookingsSummaryAsync(int mentorId);
        Task<BookingDetailResponse> GetMentorSessionDetailAsync(int mentorId, int sessionId);
        Task CancelBookingAsync(int bookingId, int candidateId);
        Task RateMentorAsync(int bookingId, int candidateId, RateMentorRequest request);
        Task<List<CandidateSessionSummaryResponse>> GetCandidateCompletedBookingsSummaryAsync(int candidateId);
        Task<BookingDetailResponse> GetCandidateSessionDetailAsync(int candidateId, int sessionId);
    }
}
