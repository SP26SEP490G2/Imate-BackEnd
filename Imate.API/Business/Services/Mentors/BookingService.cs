using Microsoft.EntityFrameworkCore;
using Imate.API.Business.Interfaces.Mentors;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Imate.API.Presentation.RequestModels.Mentors;
using Imate.API.Presentation.ResponseModels.Mentors;
using Imate.API.Business.Exceptions;

namespace Imate.API.Business.Services.Mentors
{
    public class BookingService : IBookingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private const string LocalTimeZoneId = "SE Asia Standard Time";

        public BookingService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<BookingResponseModel> CreateBookingAsync(BookingCreateRequest request, int candidateId)
        {
            // 1. Validation
            var mentor = await _unitOfWork.Mentors.GetByIdAsync(request.MentorId)
                ?? throw new NotFoundException("Mentor not found.");

            var mentorAccount = await _unitOfWork.Accounts.GetByIdAsync(request.MentorId)
                ?? throw new NotFoundException("Mentor account not found.");

            var candidateAccount = await _unitOfWork.Accounts.GetByIdAsync(candidateId)
                ?? throw new NotFoundException("Candidate account not found.");

            // Check if slot exists and belongs to mentor
            var isMentorSlot = await _unitOfWork.Bookings.HasMentorRecurringSlotAsync(request.MentorId, request.SlotId);
            if (!isMentorSlot)
            {
                throw new BadRequestException("This slot does not belong to the mentor or is inactive.");
            }

            var slot = await _unitOfWork.Slots.GetByIdAsync(request.SlotId)
                ?? throw new NotFoundException("Slot not found.");

            // Check Date Range (Next 14 days)
            var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(LocalTimeZoneId));
            var todayLocal = DateOnly.FromDateTime(nowLocal);
            var maxDateLocal = todayLocal.AddDays(14);

            if (request.BookDate < todayLocal || request.BookDate > maxDateLocal)
            {
                throw new BadRequestException("Booking date must be within the next 14 days.");
            }

            // Calculate UTC StartTime
            TimeZoneInfo localTimeZone = TimeZoneInfo.FindSystemTimeZoneById(LocalTimeZoneId);
            var localDateTimeStart = request.BookDate.ToDateTime(slot.StartTime);
            var startUtc = TimeZoneInfo.ConvertTimeToUtc(localDateTimeStart, localTimeZone);

            // Advance hours (min 6 hours)
            if (startUtc < DateTime.UtcNow.AddHours(6))
            {
                throw new BadRequestException("Booking must be made at least 6 hours in advance.");
            }

            // Check Availability
            var isAvailable = await _unitOfWork.Bookings.IsSlotAvailableAsync(request.MentorId, request.SlotId, request.BookDate);
            if (!isAvailable)
            {
                throw new BadRequestException("This slot is already booked for the selected date.");
            }

            // Check if candidate has overlapping bookings
            var duration = slot.EndTime.ToTimeSpan() - slot.StartTime.ToTimeSpan();
            var endUtc = startUtc.Add(duration);
            var isCandidateBusy = await _unitOfWork.Bookings.HasCandidateBookingAtTimeAsync(candidateId, startUtc, endUtc);
            if (isCandidateBusy)
            {
                throw new BadRequestException("You already have another confirmed booking at this time.");
            }

            // 2. Financials
            int price = mentor.PricePerSession;

            if (candidateAccount.Balance < price)
            {
                throw new BadRequestException($"Số dư không đủ để đặt lịch. Cần {price:N0} imCoin, hiện có {candidateAccount.Balance:N0} imCoin.");
            }

            // Deduct balance (tracked by EF, will save with SaveChangesAsync below)
            candidateAccount.Balance -= price;

            // Create Escrow Transaction
            var transaction = new Transaction
            {
                SourceAccountId = candidateId,
                TargetAccountId = request.MentorId,
                TransactionType = TransactionType.BookingFee,
                Amount = price,
                Status = TransactionStatus.Escrow,
                EscrowDeadline = startUtc.AddHours(24), // Auto-release 24h after start if no complaints (example logic)
                CreatedAt = DateTime.UtcNow
            };

            // 3. Persistence
            var booking = new Booking
            {
                CandidateId = candidateId,
                MentorId = request.MentorId,
                StartTime = startUtc,
                BookDate = request.BookDate,
                PriceAtBooking = price,
                Status = BookingStatus.Confirmed,
                AgoraChannelName = "temp", // Temporary value, will be updated to booking.Id
                CreatedAt = DateTime.UtcNow
            };

            // Link transaction to booking using navigation property for proper ID handling
            booking.Transactions.Add(transaction);

            await _unitOfWork.Bookings.AddAsync(booking);
            
            await _unitOfWork.SaveChangesAsync();

            // Setup Agora channel using generated ID
            booking.AgoraChannelName = booking.Id.ToString();
            await _unitOfWork.SaveChangesAsync();

            return new BookingResponseModel
            {
                Id = booking.Id,
                MentorName = mentorAccount.FullName,
                StartTime = booking.StartTime,
                Price = booking.PriceAtBooking,
                Status = booking.Status
            };
        }

        public async Task<List<MentorBookedSlotResponse>> GetBookedSlotsByMentorIdAsync(int mentorId)
        {
            var bookings = await _unitOfWork.Bookings.GetMentorUpcomingBookingsAsync(mentorId, DateTime.UtcNow, DateTime.UtcNow.AddDays(30));
            
            return bookings.Select(b => new MentorBookedSlotResponse
            {
                BookingId = b.Id,
                CandidateId = b.CandidateId,
                CandidateName = b.Candidate.FullName,
                CandidateAvatarUrl = b.Candidate.AvatarUrl,
                StartTime = b.StartTime,
                BookDate = b.BookDate,
                Status = b.Status
            }).ToList();
        }

        public async Task<List<BookingDetailResponse>> GetCandidateBookingsAsync(int candidateId)
        {
            var bookings = await _unitOfWork.Bookings.GetAllBookings()
                .Where(b => b.CandidateId == candidateId)
                .Select(b => new
                {
                    b.Id,
                    b.MentorId,
                    b.CandidateId,
                    ProfileName = b.Mentor.Account.FullName,
                    ProfileAvatarUrl = b.Mentor.Account.AvatarUrl,
                    b.StartTime,
                    b.BookDate,
                    b.Status,
                    b.AgoraChannelName,
                    b.PriceAtBooking
                })
                .ToListAsync();

            var slots = await _unitOfWork.Slots.FindAll(false).ToListAsync();
            TimeZoneInfo localTimeZone = TimeZoneInfo.FindSystemTimeZoneById(LocalTimeZoneId);

            return bookings.Select(b =>
            {
                var localStartTime = TimeZoneInfo.ConvertTimeFromUtc(b.StartTime.DateTime, localTimeZone);
                var timeOnly = TimeOnly.FromDateTime(localStartTime);
                var slot = slots.FirstOrDefault(s => s.DayOfWeek == (int)b.BookDate.DayOfWeek && s.StartTime == timeOnly);
                
                DateTimeOffset endTime = b.StartTime.AddHours(1); // fallback
                if (slot != null)
                {
                    endTime = b.StartTime.Add(slot.EndTime.ToTimeSpan() - slot.StartTime.ToTimeSpan());
                }

                return new BookingDetailResponse
                {
                    BookingId = b.Id,
                    MentorId = b.MentorId,
                    CandidateId = b.CandidateId,
                    ProfileName = b.ProfileName,
                    ProfileAvatarUrl = b.ProfileAvatarUrl,
                    JobTitle = "Mentor",
                    StartTime = b.StartTime,
                    EndTime = endTime,
                    BookDate = b.BookDate,
                    Status = b.Status,
                    MeetingRoomId = b.AgoraChannelName,
                    Price = b.PriceAtBooking
                };
            }).ToList();
        }

        public async Task<List<BookingDetailResponse>> GetMentorBookingsAsync(int mentorId)
        {
            var bookings = await _unitOfWork.Bookings.GetAllBookings()
                .Where(b => b.MentorId == mentorId)
                .Select(b => new
                {
                    b.Id,
                    b.MentorId,
                    b.CandidateId,
                    ProfileName = b.Candidate.FullName,
                    ProfileAvatarUrl = b.Candidate.AvatarUrl,
                    b.StartTime,
                    b.BookDate,
                    b.Status,
                    b.AgoraChannelName,
                    b.PriceAtBooking
                })
                .ToListAsync();

            var slots = await _unitOfWork.Slots.FindAll(false).ToListAsync();
            TimeZoneInfo localTimeZone = TimeZoneInfo.FindSystemTimeZoneById(LocalTimeZoneId);

            return bookings.Select(b =>
            {
                var localStartTime = TimeZoneInfo.ConvertTimeFromUtc(b.StartTime.DateTime, localTimeZone);
                var timeOnly = TimeOnly.FromDateTime(localStartTime);
                var slot = slots.FirstOrDefault(s => s.DayOfWeek == (int)b.BookDate.DayOfWeek && s.StartTime == timeOnly);
                
                DateTimeOffset endTime = b.StartTime.AddHours(1); // fallback
                if (slot != null)
                {
                    endTime = b.StartTime.Add(slot.EndTime.ToTimeSpan() - slot.StartTime.ToTimeSpan());
                }

                return new BookingDetailResponse
                {
                    BookingId = b.Id,
                    MentorId = b.MentorId,
                    CandidateId = b.CandidateId,
                    ProfileName = b.ProfileName,
                    ProfileAvatarUrl = b.ProfileAvatarUrl,
                    JobTitle = "Candidate",
                    StartTime = b.StartTime,
                    EndTime = endTime,
                    BookDate = b.BookDate,
                    Status = b.Status,
                    MeetingRoomId = b.AgoraChannelName,
                    Price = b.PriceAtBooking
                };
            }).ToList();
        }

        public async Task CancelBookingAsync(int bookingId, int candidateId)
        {
            var booking = await _unitOfWork.Bookings.GetBookingByIdAsync(bookingId)
                ?? throw new NotFoundException("Booking not found.");

            if (booking.CandidateId != candidateId)
            {
                throw new BadRequestException("You are not authorized to cancel this booking.");
            }

            if (booking.Status != BookingStatus.Confirmed)
            {
                throw new BadRequestException($"Cannot cancel booking with status {booking.Status}.");
            }

            // Cancellation deadline: at least 6 hours before StartTime
            if (booking.StartTime < DateTime.UtcNow.AddHours(6))
            {
                throw new BadRequestException("You can only cancel bookings at least 6 hours before the start time.");
            }

            booking.Status = BookingStatus.Cancelled;
            booking.UpdatedAt = DateTime.UtcNow;
            // No need to call UpdateAsync as it's already tracked.

            // Handle Point Refund
            // Find the associated transaction
            var transaction = await _unitOfWork.Transactions.GetBookingTransactionAsync(bookingId);

            if (transaction != null && transaction.Status == TransactionStatus.Escrow)
            {
                transaction.Status = TransactionStatus.Cancelled;
                transaction.UpdatedAt = DateTime.UtcNow;
                // No need to call UpdateAsync as it's tracked and SaveChangesAsync will catch it.
                // Using UpdateAsync(transaction) here would trigger the tracking conflict.

                // Assuming points should be returned to candidate balance
                var candidateAccount = await _unitOfWork.Accounts.GetByIdAsync(candidateId);
                if (candidateAccount != null)
                {
                    candidateAccount.Balance += transaction.Amount;
                    await _unitOfWork.Accounts.UpdateAsync(candidateAccount);
                }
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task RateMentorAsync(int bookingId, int candidateId, RateMentorRequest request)
        {
            var booking = await _unitOfWork.Bookings.GetBookingByIdAsync(bookingId)
                ?? throw new NotFoundException("Booking not found.");

            if (booking.CandidateId != candidateId)
            {
                throw new BadRequestException("You are not authorized to rate this booking.");
            }

            if (booking.Status != BookingStatus.Completed)
            {
                throw new BadRequestException("Only completed bookings can be rated.");
            }

            if (booking.RatingCreatedAt != null)
            {
                throw new BadRequestException("This booking has already been rated.");
            }

            // Update booking with rating
            booking.RatingScore = request.RatingScore;
            booking.ReviewText = request.ReviewText;
            booking.RatingCreatedAt = DateTime.UtcNow;
            booking.UpdatedAt = DateTime.UtcNow;

            // Recalculate Mentor's average rating
            var mentor = await _unitOfWork.Mentors.GetMentorByIdAsync(booking.MentorId)
                ?? throw new NotFoundException("Mentor not found.");

            var completedBookings = await _unitOfWork.Bookings.GetMentorCompletedBookingsAsync(booking.MentorId);
            var ratedBookings = completedBookings.Where(b => b.RatingScore.HasValue || b.Id == bookingId).ToList();

            if (ratedBookings.Any())
            {
                mentor.TotalRatingCount = ratedBookings.Count;
                mentor.AvgRatings = (decimal)ratedBookings.Average(b => b.Id == bookingId ? request.RatingScore : b.RatingScore!.Value);
            }

            await _unitOfWork.SaveChangesAsync();
        }
    }
}
