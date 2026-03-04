using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

namespace Imate.API.Infrastructure.Configurations
{
    public static class FirebaseServiceExtensions
    {
        public static IServiceCollection AddFirebaseAdmin(this IServiceCollection services)
        {
            var firebaseServiceAccountKeyPath = Path.Combine(AppContext.BaseDirectory, "serviceAccountKey.json");

            if (!File.Exists(firebaseServiceAccountKeyPath))
            {
                throw new FileNotFoundException($"Firebase service account key file not found at: {firebaseServiceAccountKeyPath}");
            }

            if (FirebaseApp.DefaultInstance == null)
            {
                FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.FromFile(firebaseServiceAccountKeyPath)
                });
            }

            return services;

        }
    }
}
