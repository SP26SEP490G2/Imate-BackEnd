using Imate.API.Presentation.RequestModels.UserManagement;
using Imate.API.Presentation.ResponseModels.UserManagement;

namespace Imate.API.Business.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterWithEmailAsync(RegisterWithEmailRequest request);
        Task<AuthResponse> VerifyFirebaseTokenAndLoginAsync(LoginRequest request);
        Task<AuthResponse> RegisterOrLoginWithGoogleAsync(RegisterWithGoogleRequest request);
        Task CreateEmployeeAccountAsync(int accountId, CreateEmployeeRequest request);
        Task ChangePasswordAsync(int accountId, ChangePasswordRequest request);
        Task<AuthResponse> RefreshTokenAsync(string refreshToken);
        Task<string> GenerateActionCodeAsync(string email, string actionType);
        Task SendActionEmailAsync(string oobCode, string email, string actionType);
    }
}
