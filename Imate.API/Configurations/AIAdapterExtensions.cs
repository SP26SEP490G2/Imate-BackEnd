using Imate.AI.Module.Configuration;
using Imate.AI.Module.Core.Interfaces;
using Imate.API.Business.Services;
using Imate.API.Business.Services.ExternalServices;
using Microsoft.Extensions.DependencyInjection;

namespace Imate.API.Configurations
{
    public static class AIAdapterExtensions
    {
        public static void RegisterAIAdapters(this IServiceCollection services)
        {
            // Đăng ký tất cả services từ AI Module
            services.AddImateAIModule();

            // Đăng ký CvDataProvider (bridge giữa API và AI Module)
            services.AddScoped<ICvDataProvider, CvDataProvider>();

            // Đăng ký QuestionDataProvider (bridge cho RAG Practice Test)
            services.AddScoped<IQuestionDataProvider, QuestionDataProvider>();

            // Đăng ký InterviewSessionDataProvider (bridge cho UC-35 Mock Interview)
            services.AddScoped<IInterviewSessionDataProvider, InterviewSessionDataProvider>();
            services.AddScoped<ITrainingJourneyDataProvider, TrainingJourneyDataProvider>();

            // Đăng ký Gemini Speech TTS (thay thế Azure)
            services.AddScoped<ISpeechSynthesisService, GeminiSpeechSynthesisService>();
            services.AddScoped<IAzureSpeechSynthesisService, AzureSpeechSynthesisService>();

            // HttpClient cho Gemini TTS
            services.AddHttpClient();

            // MemoryCache cho TTS caching
            services.AddMemoryCache();
        }
    }
}

