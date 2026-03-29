using Imate.API.DataAccess.Interfaces;
using Imate.API.Models.Enums;

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
        /// How long after the booking start time before we consider it "expired".
        /// Default: 2 hours. This gives a generous buffer for long sessions.
        /// </summary>
        private static readonly TimeSpan ExpirationBuffer = TimeSpan.FromHours(2);

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
            Console.WriteLine("🕐 AutoCompleteBookingService: Starting in 10 seconds...");
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            Console.WriteLine("🚀 AutoCompleteBookingService: Service started. Scanning immediately...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessExpiredBookingsAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ AutoCompleteBookingService ERROR: {ex.Message}");
                    _logger.LogError(ex, "AutoCompleteBookingService encountered an error.");
                }

                Console.WriteLine($"⏳ AutoCompleteBookingService: Next scan in {ScanInterval.TotalMinutes} minutes...");
                await Task.Delay(ScanInterval, stoppingToken);
            }
        }

        private async Task ProcessExpiredBookingsAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            // Cutoff: any Confirmed booking whose StartTime is older than now - buffer
            var cutoffTime = DateTime.UtcNow.Subtract(ExpirationBuffer);

            var expiredBookings = await unitOfWork.Bookings
                .GetExpiredConfirmedBookingsAsync(cutoffTime);

            if (!expiredBookings.Any())
            {
                return;
            }

            _logger.LogInformation(
                "AutoCompleteBooking: Found {Count} expired Confirmed booking(s). Processing...",
                expiredBookings.Count());

            foreach (var booking in expiredBookings)
            {
                try
                {
                    // Mark booking as Completed
                    booking.Status = BookingStatus.Completed;
                    booking.UpdatedAt = DateTime.UtcNow;

                    // Release Escrow → pay the Mentor
                    var escrowTransaction = await unitOfWork.Transactions
                        .GetBookingTransactionAsync(booking.Id);

                    if (escrowTransaction != null && escrowTransaction.Status == TransactionStatus.Escrow)
                    {
                        escrowTransaction.Status = TransactionStatus.Released;
                        escrowTransaction.UpdatedAt = DateTime.UtcNow;

                        // Credit Mentor's balance
                        var mentorAccount = await unitOfWork.Accounts
                            .GetByIdAsync(booking.MentorId);

                        if (mentorAccount != null)
                        {
                            mentorAccount.Balance += escrowTransaction.Amount;
                        }
                    }

                    _logger.LogInformation(
                        "AutoCompleteBooking: Booking #{BookingId} (StartTime: {StartTime}) → Completed. Escrow released: {HasEscrow}",
                        booking.Id, booking.StartTime, escrowTransaction != null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "AutoCompleteBooking: Failed to process Booking #{BookingId}",
                        booking.Id);
                }
            }

            await unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "AutoCompleteBooking: Successfully processed {Count} booking(s).",
                expiredBookings.Count());
        }
    }
}
