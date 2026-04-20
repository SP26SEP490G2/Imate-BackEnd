using FirebaseAdmin.Auth;

namespace Imate.API.Business.Interfaces.ExternalServices
{
    public interface IFirebaseAuthService
    {
        Task<UserRecord> CreateUserAsync(UserRecordArgs args);

        Task<FirebaseToken> VerifyIdTokenAsync(string idToken);

        Task<string> GeneratePasswordResetLinkAsync(string email);

        Task DeleteUserAsync(string uid);

        Task<UserRecord> UpdateUserAsync(UserRecordArgs args);

        Task<string> GenerateEmailVerificationLinkAsync(string email);
    }
}
