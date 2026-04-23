using Microsoft.EntityFrameworkCore;
using Imate.API.DataAccess.Interfaces.Mentors;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Imate.API.Presentation.ResponseModels.Mentors;
using Imate.API.DataAccess.ApplicationDbContext;

namespace Imate.API.DataAccess.Repositories.Mentors
{
    public class BookingRepository : IBookingRepository
    {
        private readonly ImateDbContext _context;

        public BookingRepository(ImateDbContext context)
        {
            _context = context;
        }

        public async Task<Booking> AddAsync(Booking booking)
        {
            await _context.Bookings.AddAsync(booking);
            return booking;
        }

        public async Task<bool> IsSlotAvailableAsync(int mentorId, int slotId, DateOnly bookDate)
        {
            const string LocalTimeZoneId = "SE Asia Standard Time";
            var slot = await _context.Slots.FindAsync(slotId);
            if (slot == null)
            {
                return false;
            }

            // Check DayOfWeek match - use consistent comparison
            var bookingDayOfWeek = (int)bookDate.DayOfWeek;
            if (slot.DayOfWeek != bookingDayOfWeek)
            {
                return false;
            }

            // Fix: Tính StartTime giống như BookingService
            TimeZoneInfo localTimeZone = TimeZoneInfo.FindSystemTimeZoneById(LocalTimeZoneId);
            var localDateTimeStart = bookDate.ToDateTime(slot.StartTime);
            var slotStartDateTime = TimeZoneInfo.ConvertTimeToUtc(localDateTimeStart, localTimeZone);

            // Check for existing booking (Confirmed, Pending, or Completed all occupy the slot)
            var occupyingStatuses = new[] { BookingStatus.Confirmed, BookingStatus.Pending, BookingStatus.Completed };
            var existingBooking = await _context.Bookings
                .AnyAsync(b => b.MentorId == mentorId
                    && b.BookDate == bookDate
                    && b.StartTime == slotStartDateTime
                    && occupyingStatuses.Contains(b.Status));

            if (existingBooking)
            {
                return false;
            }

            return true;
        }

        public async Task<bool> HasCandidateBookingAtTimeAsync(int candidateId, DateTime startTime, DateTime endTime)
        {
            var bookDate = DateOnly.FromDateTime(startTime);
            var bookingDayOfWeek = (int)bookDate.DayOfWeek;

            // Get all confirmed bookings for this candidate on the same date
            var candidateBookings = await _context.Bookings
                .Where(b => b.CandidateId == candidateId
                    && b.Status == BookingStatus.Confirmed
                    && b.BookDate == bookDate)
                .ToListAsync();

            if (!candidateBookings.Any())
            {
                return false;
            }

            // Get all slots for this day of week
            var slotsForDay = await _context.Slots
                .Where(s => s.DayOfWeek == bookingDayOfWeek)
                .ToListAsync();

            // Check each existing booking for overlap
            foreach (var booking in candidateBookings)
            {
                var bookingTimeOnly = TimeOnly.FromDateTime(booking.StartTime.DateTime);
                
                // Find the slot that matches this booking's StartTime
                var slot = slotsForDay.FirstOrDefault(s => s.StartTime == bookingTimeOnly);

                if (slot != null)
                {
                    // Calculate the end time of this existing booking
                    var existingBookingEndTime = booking.StartTime.Add(slot.EndTime.ToTimeSpan() - slot.StartTime.ToTimeSpan());

                    // Check if there's any overlap between the new booking and existing booking
                    // Two time ranges overlap if: newStart < existingEnd AND newEnd > existingStart
                    if (startTime < existingBookingEndTime && endTime > booking.StartTime)
                    {
                        return true; // Found overlapping booking
                    }
                }
                else
                {
                    // If we can't find the slot, check for exact StartTime match as fallback
                    if (booking.StartTime == startTime)
                    {
                        return true;
                    }
                }
            }

            return false; // No overlapping bookings found
        }

        public async Task<Booking> GetBookingByIdAsync(int bookingId)
        {
            return await _context.Bookings
                .Include(b => b.Candidate)
                .Include(b => b.Mentor)
                    .ThenInclude(m => m.Account)
                .Include(b => b.Transactions)
                .FirstOrDefaultAsync(b => b.Id == bookingId);
        }

        public async Task<bool> HasMentorRecurringSlotAsync(int mentorId, int slotId)
        {
            return await _context.MentorRecurringSlots
                .AnyAsync(mrs => mrs.MentorId == mentorId
                    && mrs.SlotId == slotId
                    && mrs.IsActive);
        }

        public async Task<IEnumerable<Booking>> GetMentorUpcomingBookingsAsync(int mentorId, DateTime fromDate, DateTime toDate)
        {
            return await _context.Bookings
                .Include(b => b.Candidate)
                .Where(b => b.MentorId == mentorId
                    && b.StartTime >= fromDate
                    && b.StartTime <= toDate
                    && b.Status == BookingStatus.Confirmed)
                .OrderBy(b => b.StartTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Booking>> GetCandidateUpcomingBookingsAsync(int candidateId, DateTime fromDate, DateTime toDate)
        {
            return await _context.Bookings
                .Include(b => b.Mentor)
                    .ThenInclude(m => m.Account)
                .Where(b => b.CandidateId == candidateId
                    && b.StartTime >= fromDate
                    && b.StartTime <= toDate
                    && b.Status == BookingStatus.Confirmed) // Only get Confirmed bookings
                .OrderBy(b => b.StartTime)
                .ToListAsync();
        }

        public async Task<Booking> UpdateAsync(Booking booking)
        {
            _context.Bookings.Update(booking);
            return booking;
        }

        public async Task<List<ReviewResponseModel>> GetMappedReviewsByMentorIdAsync(int mentorId)
        {
            var reviews = await _context.Bookings
                .Where(b => b.MentorId == mentorId && 
                           b.RatingScore != null && 
                           b.ReviewText != null &&
                           b.RatingCreatedAt != null)
                .Join(
                    _context.Accounts, // Bảng để join
                    booking => booking.CandidateId, // Khóa ngoại từ Bookings
                    account => account.Id,          // Khóa chính từ Accounts
                    (booking, candidateAccount) => new ReviewResponseModel // Map kết quả
                    {
                        Score = booking.RatingScore.Value,
                        Text = booking.ReviewText,
                        CreatedAt = booking.RatingCreatedAt.Value,
                        ReviewerFullName = candidateAccount.FullName,
                        ReviewerAvatarUrl = candidateAccount.AvatarUrl
                    }
                )
                .Where(r => r.CreatedAt != null && 
                           !string.IsNullOrWhiteSpace(r.ReviewerFullName) && 
                           !string.IsNullOrWhiteSpace(r.ReviewerAvatarUrl))
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return reviews;
        }

        public async Task<int> CountBookingsByCandidateIdAsync(int candidateId)
        {
            // Đếm tất cả các booking của CandidateId này
            // (Bạn có thể thêm điều kiện .Where(b => b.Status == BookingStatus.Completed) 
            // nếu bạn chỉ muốn đếm các buổi đã hoàn thành)
            return await _context.Bookings
                .CountAsync(b => b.CandidateId == candidateId && b.MentorId != null);
        }

        public async Task<int> CountBookingsCompletedByCandidateIdAsync(int candidateId)
        {
            // Đếm tất cả các booking của CandidateId này
            // (Bạn có thể thêm điều kiện .Where(b => b.Status == BookingStatus.Completed) 
            // nếu bạn chỉ muốn đếm các buổi đã hoàn thành)
            return await _context.Bookings.Where(b => b.Status == Models.Enums.BookingStatus.Completed)
                .CountAsync(b => b.CandidateId == candidateId && b.MentorId != null);
        }

        public async Task<int> CountCompletedBookingsByMentorIdAsync(int mentorId)
        {

            return await _context.Bookings
                .CountAsync(b => b.MentorId == mentorId && b.Status == Models.Enums.BookingStatus.Completed);
        }

        public async Task<IEnumerable<Booking>> GetMentorCompletedBookingsAsync(int mentorId)
        {
            return await _context.Bookings
                .Include(b => b.Candidate)
                .Where(b => b.MentorId == mentorId
                    && b.Status == BookingStatus.Completed)
                .OrderByDescending(b => b.StartTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Booking>> GetCandidateCompletedBookingsAsync(int candidateId)
        {
            return await _context.Bookings
                .Include(b => b.Mentor)
                    .ThenInclude(m => m.Account)
                .Where(b => b.CandidateId == candidateId
                    && b.Status == BookingStatus.Completed)
                .OrderByDescending(b => b.StartTime)
                .ToListAsync();
        }
        public IQueryable<Booking> GetAllBookings()
        {
            return _context.Bookings.AsQueryable()
                .Include(a => a.Mentor)
                .Include(a => a.Applications)
                .Include(a => a.Candidate);
        }


        public async Task<List<RatingDetailModel>> GetCandidateRatingsByMentorIdAsync(int mentorId)
        {
            var ratings = await _context.Bookings
                .Where(b => b.MentorId == mentorId && b.RatingScore != null)
                .Join(
                    _context.Accounts,
                    booking => booking.CandidateId,
                    account => account.Id,
                    (booking, candidateAccount) => new RatingDetailModel
                    {
                        BookingId = booking.Id,
                        CandidateAvatar = candidateAccount.AvatarUrl ?? string.Empty,
                        CandidateName = candidateAccount.FullName,
                        ReviewText = booking.ReviewText ?? string.Empty,
                        RatingScore = booking.RatingScore.Value,
                        CreatedAt = booking.RatingCreatedAt ?? booking.CreatedAt
                    }
                )
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return ratings;
        }

        public async Task<IEnumerable<Booking>> GetConfirmedBookingsBySlotIdAsync(int mentorId, int slotId)
        {
            const string LocalTimeZoneId = "SE Asia Standard Time";
            // Lấy thông tin slot
            var slot = await _context.Slots.FindAsync(slotId);
            if (slot == null)
            {
                return Enumerable.Empty<Booking>();
            }

            var slotStartTime = slot.StartTime;
            var slotDayOfWeek = slot.DayOfWeek;
            var currentDateTime = DateTime.UtcNow;

            TimeZoneInfo localTimeZone = TimeZoneInfo.FindSystemTimeZoneById(LocalTimeZoneId);

            // Tìm tất cả bookings của mentor có:
            // - BookDate.DayOfWeek == slot.DayOfWeek
            // - Status == Confirmed, Pending, hoặc Completed
            // - StartTime >= DateTime.Now (chỉ lấy bookings tương lai)
            var occupyingStatuses = new[] { BookingStatus.Confirmed, BookingStatus.Pending, BookingStatus.Completed };
            var bookings = await _context.Bookings
                .Include(b => b.Candidate)
                .Where(b => b.MentorId == mentorId
                    && occupyingStatuses.Contains(b.Status)
                    && b.StartTime >= currentDateTime
                    && (int)b.BookDate.DayOfWeek == slotDayOfWeek)
                .ToListAsync();

            // Fix: Filter by converting UTC StartTime to local time for comparison
            return bookings.Where(b =>
            {
                var localStartTime = TimeZoneInfo.ConvertTimeFromUtc(b.StartTime.DateTime, localTimeZone);
                return TimeOnly.FromDateTime(localStartTime) == slotStartTime;
            }).OrderBy(b => b.StartTime);
        }

        public async Task<IEnumerable<Booking>> GetExpiredConfirmedBookingsAsync(DateTime cutoffTimeUtc)
        {
            return await _context.Bookings
                .Where(b => b.Status == BookingStatus.Confirmed
                    && b.StartTime < cutoffTimeUtc)
                .ToListAsync();
        }

        public async Task<IEnumerable<Booking>> GetBookingsPendingEscrowReleaseAsync(DateTime nowUtc)
        {
            return await _context.Bookings
                .Include(b => b.Transactions)
                .Where(b => (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed)
                    && b.Transactions.Any(t => t.Status == TransactionStatus.Escrow && t.EscrowDeadline < nowUtc))
                .ToListAsync();
        }
    }
}
