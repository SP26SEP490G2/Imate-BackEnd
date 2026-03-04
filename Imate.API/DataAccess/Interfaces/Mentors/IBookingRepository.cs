using Imate.API.Models.Entities;
using Imate.API.Presentation.ResponseModels.Mentors;

namespace Imate.API.DataAccess.Interfaces.Mentors
{
    public interface IBookingRepository
    {
        Task<Booking> AddAsync(Booking booking);
        Task<bool> IsSlotAvailableAsync(int mentorId, int slotId, DateOnly bookDate);
        Task<bool> HasCandidateBookingAtTimeAsync(int candidateId, DateTime startTime, DateTime endTime);
        Task<Booking> GetBookingByIdAsync(int bookingId);
        Task<bool> HasMentorRecurringSlotAsync(int mentorId, int slotId);
        Task<IEnumerable<Booking>> GetMentorUpcomingBookingsAsync(int mentorId, DateTime fromDate, DateTime toDate);
        Task<IEnumerable<Booking>> GetCandidateUpcomingBookingsAsync(int candidateId, DateTime fromDate, DateTime toDate);
        Task<Booking> UpdateAsync(Booking booking);
        Task<List<ReviewResponseModel>> GetMappedReviewsByMentorIdAsync(int mentorId);
        Task<int> CountBookingsByCandidateIdAsync(int candidateId);
        Task<int> CountBookingsCompletedByCandidateIdAsync(int candidateId);
        Task<int> CountCompletedBookingsByMentorIdAsync(int mentorId);
        Task<IEnumerable<Booking>> GetMentorCompletedBookingsAsync(int mentorId);
        Task<IEnumerable<Booking>> GetCandidateCompletedBookingsAsync(int candidateId);
        IQueryable<Booking> GetAllBookings();
        Task<List<RatingDetailModel>> GetCandidateRatingsByMentorIdAsync(int mentorId);
        Task<IEnumerable<Booking>> GetConfirmedBookingsBySlotIdAsync(int mentorId, int slotId);
    }
}
