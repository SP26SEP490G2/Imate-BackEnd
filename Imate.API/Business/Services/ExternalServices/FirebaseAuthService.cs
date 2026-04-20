using FirebaseAdmin.Auth;
using Imate.API.Business.Interfaces.ExternalServices;

namespace Imate.API.Business.Services.ExternalServices
{
    public class FirebaseAuthService: IFirebaseAuthService
    {
        private readonly FirebaseAuth _firebaseAuth;

        public FirebaseAuthService(FirebaseAuth firebaseAuth)
        {
            _firebaseAuth = firebaseAuth;
        }

        public Task<UserRecord> CreateUserAsync(UserRecordArgs args)
            => _firebaseAuth.CreateUserAsync(args);

        public Task<FirebaseToken> VerifyIdTokenAsync(string idToken)
            => _firebaseAuth.VerifyIdTokenAsync(idToken);

        public Task<string> GeneratePasswordResetLinkAsync(string email)
            => _firebaseAuth.GeneratePasswordResetLinkAsync(email);

        public Task DeleteUserAsync(string uid)
            => _firebaseAuth.DeleteUserAsync(uid);

        public Task<UserRecord> UpdateUserAsync(UserRecordArgs args)
            => _firebaseAuth.UpdateUserAsync(args);

        public Task<string> GenerateEmailVerificationLinkAsync(string email)
            => _firebaseAuth.GenerateEmailVerificationLinkAsync(email);
    }
}
