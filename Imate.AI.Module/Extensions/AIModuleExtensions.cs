using Imate.AI.Module.Interfaces;
using Imate.AI.Module.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Imate.AI.Module.Extensions
{
    /// <summary>
    /// Extension methods để đăng ký AI Module services vào DI container
    /// Host project gọi services.AddImateAIModule() trong Program.cs
    /// </summary>
    public static class AIModuleExtensions
    {
        /// <summary>
        /// Đăng ký tất cả services của AI Module
        /// </summary>
        public static IServiceCollection AddImateAIModule(this IServiceCollection services)
        {
            // Gemini AI external service (uses HttpClient)
            services.AddHttpClient<IGeminiService, GeminiService>();

            // CV Analysis business service
            services.AddScoped<ICvAnalysisService, CvAnalysisService>();

            // Practice Test service (UC-30)
            services.AddScoped<IPracticeTestService, PracticeTestService>();

            // Interview AI service (UC-35)
            services.AddScoped<IInterviewService, InterviewService>();

            return services;
        }
    }
}
