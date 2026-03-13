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
                throw new BadRequestException("Insufficient balance to book this session.");
            }

            // Deduct balance
            candidateAccount.Balance -= price;
            await _unitOfWork.Accounts.UpdateAsync(candidateAccount);

            // Create Escrow Transaction
            var transaction = new Transaction
            {
                SourceAccountId = candidateId,
                TargetAccountId = request.MentorId,
                TransactionType = TransactionType.Booking,
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
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Bookings.AddAsync(booking);
            
            // Link transaction to booking
            transaction.BookingId = booking.Id;
            _unitOfWork.Transactions.AddAsync(transaction);

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
    }
}
