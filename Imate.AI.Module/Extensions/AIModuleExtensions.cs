using Imate.AI.Module.Agents;
using Imate.AI.Module.AIServices;
using Imate.AI.Module.Interfaces.Agents;
using Imate.AI.Module.Interfaces.AIServices;
using Imate.AI.Module.Interfaces.Orchestrators;
using Imate.AI.Module.Orchestrators;
using Microsoft.Extensions.DependencyInjection;

namespace Imate.AI.Module.Extensions
{
    /// <summary>
    /// Extension methods để đăng ký AI Module services vào DI container
    /// Host project gọi services.AddImateAIModule() trong Program.cs
    /// 
    /// Kiến trúc 4 tầng:
    ///   Controllers → Orchestrators → Agents → AI Services
    /// </summary>
    public static class AIModuleExtensions
    {
        /// <summary>
        /// Đăng ký tất cả services của AI Module theo kiến trúc 4 tầng
        /// </summary>
        public static IServiceCollection AddImateAIModule(this IServiceCollection services)
        {
            // ═══════════════════════════════════════════
            // Tầng 4: AI Services (External API calls)
            // ═══════════════════════════════════════════
            services.AddHttpClient<IGeminiService, GeminiService>();

            // ═══════════════════════════════════════════
            // Tầng 3: Agents (Domain logic, prompt engineering)
            // ═══════════════════════════════════════════
            services.AddScoped<IInterviewAgent, InterviewAgent>();
            services.AddScoped<IFeedbackAgent, FeedbackAgent>();
            services.AddScoped<ICvAnalysisAgent, CvAnalysisAgent>();
            services.AddScoped<IPracticeTestAgent, PracticeTestAgent>();

            // ═══════════════════════════════════════════
            // Tầng 2: Orchestrators (Workflow coordination)
            // ═══════════════════════════════════════════
            services.AddScoped<IInterviewOrchestrator, InterviewOrchestrator>();
            services.AddScoped<ICvAnalysisOrchestrator, CvAnalysisOrchestrator>();
            services.AddScoped<IPracticeTestOrchestrator, PracticeTestOrchestrator>();

            return services;
        }
    }
}
