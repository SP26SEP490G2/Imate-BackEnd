using Imate.API.Business.Helper;
using Imate.API.Business.Interfaces;
using Imate.API.Business.Interfaces.Notification;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Imate.API.Presentation.SignalR.Events.Transactions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Imate.API.Business.Services.Mentors
{
    /// <summary>
    /// Background service that periodically scans for Confirmed bookings
    /// whose scheduled time has long passed, and auto-completes them.
    /// This prevents bookings from being stuck at "Confirmed" forever
    /// when users forget to stop recording or never join the call.
    /// </summary>
    public class AutoCompleteBookingService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AutoCompleteBookingService> _logger;
        private readonly IMediator _mediator;

        /// <summary>
        /// How often the job runs (every 30 minutes).
        /// </summary>
        private static readonly TimeSpan ScanInterval = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Buffers time to wait after session end before auto-completing.
        /// </summary>
        private static readonly TimeSpan ExpirationBuffer = TimeSpan.FromHours(1);

        public AutoCompleteBookingService(
            IServiceScopeFactory scopeFactory,
            ILogger<AutoCompleteBookingService> logger,
            IMediator mediator)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _mediator = mediator;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Wait a short moment on startup to let the app fully initialize
            Console.WriteLine("AutoCompleteBookingService: Starting in 10 seconds...");
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            Console.WriteLine("AutoCompleteBookingService: Service started. Scanning immediately...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessExpiredBookingsAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"AutoCompleteBookingService ERROR: {ex.Message}");
                    _logger.LogError(ex, "AutoCompleteBookingService encountered an error.");
                }

                Console.WriteLine($"AutoCompleteBookingService: Next scan in {ScanInterval.TotalMinutes} minutes...");
                await Task.Delay(ScanInterval, stoppingToken);
            }
        }

        private async Task ProcessExpiredBookingsAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var systemConfigService = scope.ServiceProvider.GetRequiredService<ISystemConfigService>();
            var systemNotificationService = scope.ServiceProvider.GetRequiredService<ISystemNotificationService>();
            var now = DateTime.UtcNow;

            // --- Phase 1: Auto-complete Confirmed bookings that have passed their 1h mark ---
            // (Using 1 hour as standard duration + buffer)
            var autocompleteCutoff = now.AddHours(-1);
            var confirmedBookings = await unitOfWork.Bookings.GetExpiredConfirmedBookingsAsync(autocompleteCutoff);
            
            foreach (var booking in confirmedBookings)
            {
                try {
                    booking.Status = BookingStatus.Completed;
                    booking.UpdatedAt = now;
                    _logger.LogInformation("AutoCompleteBooking: Marked Booking #{BookingId} as Completed.", booking.Id);
                } catch (Exception ex) {
                    _logger.LogError(ex, "AutoCompleteBooking: Failed to complete Booking #{BookingId}", booking.Id);
                }
            }
            await unitOfWork.SaveChangesAsync();

            // --- Phase 2: Release Escrow for Completed bookings whose report window (24h) has expired ---
            var releaseableBookings = await unitOfWork.Bookings.GetBookingsPendingEscrowReleaseAsync(now);

            foreach (var booking in releaseableBookings)
            {
                try 
                {
                    var escrowTransaction = await unitOfWork.Transactions.GetBookingTransactionAsync(booking.Id);
                    if (escrowTransaction == null || escrowTransaction.Status != TransactionStatus.Escrow) continue;

                    // Check for pending/in-review reports
                    var hasPendingReport = await unitOfWork.Applications.GetAllApplications()
                        .AnyAsync(a => a.BookingId == booking.Id 
                            && (a.ApplicationType == ApplicationType.ReportMentor || a.ApplicationType == ApplicationType.ReportRating)
                            && (a.Status == ApplicationStatus.Pending || a.Status == ApplicationStatus.InReview));

                    if (hasPendingReport)
                    {
                        _logger.LogInformation("AutoCompleteBooking: Booking #{BookingId} has a pending report. Skipping escrow release.", booking.Id);
                        continue;
                    }

                    // Release funds to mentor (with commission deduction)
                    decimal commissionRate = await systemConfigService.GetCommissionRateAsync();
                    
                    var originalAmount = escrowTransaction.Amount;
                    var commission = (int)(originalAmount * commissionRate / 100);
                    var payoutAmount = originalAmount - commission;

                    escrowTransaction.Status = TransactionStatus.Completed;
                    escrowTransaction.CommissionRateApplied = commissionRate;
                    escrowTransaction.UpdatedAt = now;

                    var mentorAccount = await unitOfWork.Accounts.GetByIdAsync(booking.MentorId);
                    if (mentorAccount != null)
                    {
                        mentorAccount.Balance += payoutAmount;
                        
                        // Create Payout Transaction record for history
                        var payoutTransaction = new Transaction
                        {
                            SourceAccountId = null, // System payout
                            TargetAccountId = mentorAccount.Id,
                            TransactionType = TransactionType.BookingPayout,
                            Amount = payoutAmount,
                            BookingId = booking.Id,
                            Status = TransactionStatus.Completed,
                            CommissionRateApplied = commissionRate,
                            Reason = $"Tự động giải ngân cho booking #{booking.Id} (Hoa hồng: {commissionRate}%)",
                            CreatedAt = now
                        };
                        payoutTransaction.EnsureExternalTransactionCode();
                        await unitOfWork.Transactions.AddAsync(payoutTransaction);
                        var balanceEvent = new BalanceUpdatedEvent(mentorAccount.Id, (decimal)mentorAccount.Balance);
                        await _mediator.Publish(balanceEvent);

                        _logger.LogInformation("AutoCompleteBooking: Released {PayoutAmount} (after {Commission} fee) to Mentor #{MentorId} for Booking #{BookingId}.", 
                            payoutAmount, commission, booking.MentorId, booking.Id);
                    }
                    
                    // Also ensure booking status is Completed if it was still Confirmed
                    if (booking.Status == BookingStatus.Confirmed) {
                        booking.Status = BookingStatus.Completed;
                        booking.UpdatedAt = now;
                    }

                    // Notifications
                    try
                    {
                        var candidateAccount = await unitOfWork.Accounts.GetByIdAsync(booking.CandidateId);
                        
                        // Notify Mentor
                        await systemNotificationService.CreateAndSendNotificationAsync(
                            booking.MentorId, 
                            $"Lịch hẹn với {candidateAccount?.FullName ?? "ứng viên"} đã hoàn thành. Bạn nhận được {payoutAmount:N0} imCoin (đã trừ {commissionRate}% hoa hồng).",
                            "/wallet"
                        );

                        // Notify Candidate
                        var mentorAccountName = mentorAccount?.FullName ?? "Mentor";
                        await systemNotificationService.CreateAndSendNotificationAsync(
                            booking.CandidateId,
                            $"Lịch hẹn với Mentor {mentorAccountName} đã được hệ thống xác nhận hoàn thành.",
                            "/candidate/interview-history"
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Failed to send auto-complete notification for booking {booking.Id}: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AutoCompleteBooking: Failed to release escrow for Booking #{BookingId}", booking.Id);
                }
            }

            await unitOfWork.SaveChangesAsync();
        }
    }
}
