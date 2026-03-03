using Imate.API.Models.Enums;

namespace Imate.API.Business.Interfaces.UserManagement
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(int accountId, IEnumerable<RoleName> roles);
        string GenerateRefreshToken();
    }
}
