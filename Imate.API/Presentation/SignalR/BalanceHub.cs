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

        // Tự động gọi khi Client kết nối thành công
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            Console.WriteLine($"[BalanceHub] Connected - ConnectionId: {Context.ConnectionId}, UserId: {userId}");

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
                await Clients.Caller.SendAsync("Connected", new { message = "Kết nối SignalR thành công" });
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            Console.WriteLine($"[BalanceHub] Disconnected - ConnectionId: {Context.ConnectionId}, UserId: {userId}");

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
