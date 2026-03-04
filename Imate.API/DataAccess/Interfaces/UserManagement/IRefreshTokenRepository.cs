using Imate.API.Models.Entities;

namespace Imate.API.DataAccess.Interfaces.UserManagement
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenAsync(string token);
        Task<RefreshToken?> GetByAccountIdAsync(int accountId);
        Task AddAsync(RefreshToken refreshToken);
        Task UpdateAsync(RefreshToken refreshToken);
        Task RevokeTokenAsync(string token);
        Task RevokeAllTokensForAccountAsync(int accountId);
        Task RemoveExpiredTokensAsync();
    }
}

