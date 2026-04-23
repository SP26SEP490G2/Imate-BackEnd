using Imate.API.DataAccess.Interfaces;
using Imate.API.Models.Enums;
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
            ILogger<AutoCompleteBookingService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
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

                    // Release funds to mentor
                    escrowTransaction.Status = TransactionStatus.Released;
                    escrowTransaction.UpdatedAt = now;

                    var mentorAccount = await unitOfWork.Accounts.GetByIdAsync(booking.MentorId);
                    if (mentorAccount != null)
                    {
                        mentorAccount.Balance += escrowTransaction.Amount;
                        _logger.LogInformation("AutoCompleteBooking: Released {Amount} to Mentor #{MentorId} for Booking #{BookingId}.", 
                            escrowTransaction.Amount, booking.MentorId, booking.Id);
                    }
                    
                    // Also ensure booking status is Completed if it was still Confirmed
                    if (booking.Status == BookingStatus.Confirmed) {
                        booking.Status = BookingStatus.Completed;
                        booking.UpdatedAt = now;
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
