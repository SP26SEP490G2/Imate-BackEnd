using Imate.AI.Module.Extensions;
using Imate.AI.Module.Interfaces;
using Imate.API.Business.Services;
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
        }
    }
}
