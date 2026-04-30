using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace Imate.API.Presentation.SignalR
{
    /// <summary>
    /// SignalR Hub quản lý session phỏng vấn AI real-time.
    /// Chức năng chính: Ngăn 2 tab cùng mở 1 session phỏng vấn.
    /// 
    /// Flow:
    ///   1. Client mở InterviewChat → gọi JoinSession(sessionId)
    ///   2. Nếu session chưa có ai → cho vào, lưu connectionId
    ///   3. Nếu session đã có tab khác → trả "SessionAlreadyActive" → client block
    ///   4. Client đóng tab → OnDisconnectedAsync → giải phóng session
    /// </summary>
    [Authorize]
    public class InterviewSessionHub : Hub
    {
        /// <summary>
        /// Tracks active sessions: sessionId → connectionId của tab đang active.
        /// Static để share giữa tất cả connections.
        /// </summary>
        private static readonly ConcurrentDictionary<int, string> _activeSessions = new();

        /// <summary>
        /// Reverse mapping: connectionId → sessionId (để cleanup khi disconnect)
        /// </summary>
        private static readonly ConcurrentDictionary<string, int> _connectionSessions = new();

        /// <summary>
        /// Client gọi khi mở trang InterviewChat.
        /// Nếu session đã có tab khác → trả lỗi, client tự block.
        /// </summary>
        public async Task JoinSession(int sessionId)
        {
            var connectionId = Context.ConnectionId;
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            // Kiểm tra session đã có tab nào đang active chưa
            if (_activeSessions.TryGetValue(sessionId, out var existingConnectionId))
            {
                // Nếu connection cũ vẫn là chính mình (reconnect) → cho phép
                if (existingConnectionId == connectionId)
                {
                    await Clients.Caller.SendAsync("SessionJoined", sessionId);
                    return;
                }

                // Session đã có tab khác → reject
                Console.WriteLine($"[InterviewHub] Session {sessionId} đã active bởi connection {existingConnectionId}. Reject {connectionId}");
                await Clients.Caller.SendAsync("SessionAlreadyActive", sessionId,
                    "Phiên phỏng vấn này đang được mở ở tab/cửa sổ khác. Vui lòng sử dụng tab đã mở.");
                return;
            }

            // Đăng ký session cho connection này
            _activeSessions[sessionId] = connectionId;
            _connectionSessions[connectionId] = sessionId;

            Console.WriteLine($"[InterviewHub] User {userId} joined session {sessionId} (connection: {connectionId})");

            // Thông báo client join thành công
            await Clients.Caller.SendAsync("SessionJoined", sessionId);
        }

        /// <summary>
        /// Client gọi khi rời trang InterviewChat (hoặc kết thúc phỏng vấn).
        /// </summary>
        public async Task LeaveSession(int sessionId)
        {
            var connectionId = Context.ConnectionId;
            CleanupSession(connectionId, sessionId);

            Console.WriteLine($"[InterviewHub] Connection {connectionId} left session {sessionId}");
            await Clients.Caller.SendAsync("SessionLeft", sessionId);
        }

        /// <summary>
        /// Tự động cleanup khi client disconnect (đóng tab, mất mạng, refresh).
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;

            if (_connectionSessions.TryRemove(connectionId, out var sessionId))
            {
                _activeSessions.TryRemove(sessionId, out _);
                Console.WriteLine($"[InterviewHub] Connection {connectionId} disconnected, released session {sessionId}");
            }

            await base.OnDisconnectedAsync(exception);
        }

        private static void CleanupSession(string connectionId, int sessionId)
        {
            // Chỉ cleanup nếu connection này đang own session đó
            if (_activeSessions.TryGetValue(sessionId, out var owner) && owner == connectionId)
            {
                _activeSessions.TryRemove(sessionId, out _);
            }
            _connectionSessions.TryRemove(connectionId, out _);
        }
    }
}
