using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Imate.API.Presentation.SignalR
{
    [Authorize]
    public class BalanceHub : Hub
    {
        // Gửi cập nhật balance cho một user cụ thể
        public async Task SendBalanceUpdate(int userId, decimal imCoinBalance)
        {
            await Clients.Group(userId.ToString()).SendAsync("BalanceUpdated", new { imCoinBalance });
        }

        // Gửi cập nhật AI Credit cho một user cụ thể
        public async Task SendAiCreditUpdate(int userId, int aiCredit)
        {
            await Clients.Group(userId.ToString()).SendAsync("AiCreditUpdated", new { aiCredit });
        }

        // Gửi cập nhật cả 2 cho một user
        public async Task SendBalanceAndAiCreditUpdate(int userId, decimal imCoinBalance, int aiCredit)
        {
            await Clients.Group(userId.ToString()).SendAsync("BalanceAndAiCreditUpdated",
                new { imCoinBalance, aiCredit });
        }

        // Helper method: Lấy User ID từ Claims
        private bool TryGetUserId(out int userId)
        {
            var userIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdClaim, out userId))
            {
                return true;
            }
            userId = 0;
            return false;
        }

        // Tự động gọi khi Client kết nối thành công
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            // LOGGING DEBUG - Giúp diagnose lỗi
            Console.WriteLine($"--- BALANCE HUB CONNECTED ---");
            Console.WriteLine($"Connection ID: {Context.ConnectionId}");
            Console.WriteLine($"User ID (Claim): {userId ?? "NULL"}");
            Console.WriteLine($"Is Authenticated: {Context.User?.Identity?.IsAuthenticated}");

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
                await Clients.Caller.SendAsync("Connected", new { message = "Kết nối SignalR thành công" });
                Console.WriteLine($"Added to group: {userId}");
            }
            else
            {
                Console.WriteLine($"ERROR: User ID is NULL or empty - Connection may fail");
                // Gửi lỗi về client
                await Clients.Caller.SendAsync("ConnectionError", new { message = "Không thể xác định User ID" });
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            Console.WriteLine($"--- BALANCE HUB DISCONNECTED ---");
            Console.WriteLine($"Connection ID: {Context.ConnectionId}");
            Console.WriteLine($"User ID: {userId ?? "NULL"}");
            if (exception != null)
            {
                Console.WriteLine($"Exception: {exception.Message}");
            }

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}