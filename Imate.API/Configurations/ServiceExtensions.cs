using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.DataAccess.Interfaces;
using Imate.API.DataAccess.Repositories;
using Imate.API.ExternalServices;
using Imate.API.BackgroundServices;
using Imate.API.Business.Interfaces.Mentors;
using Imate.API.Business.Interfaces.QuestionBank;
using Imate.API.Business.Services.Mentors;
using Imate.API.Business.Services.QuestionBank;
using Imate.API.Business.Services.Classification;
using Imate.API.Business.Interfaces.Classification;

namespace Imate.API.Configurations
{
    public static class ServiceExtensions
    {
        public static void ConfigureServices(this IServiceCollection services)
        {
            services.AddScoped<IMentorService, MentorService>();
            services.AddScoped<IQuestionService, QuestionService>();
            services.AddScoped<ICategoryService, CategoryService>();
        }

        public static void ConfigureCors(this IServiceCollection services) =>
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder =>
                    builder
                     .WithOrigins(
                    "http://localhost:7939",
                    "http://localhost:5173",
                    "http://localhost:3000",
                    "https://imate.vn",
                    "https://www.imate.vn"
                )
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
            });

        public static void ConfigureIISIntegration(this IServiceCollection services) =>
            services.Configure<IISOptions>(options =>
            {
            });

        public static void ConfigureSqlContext(this IServiceCollection services, IConfiguration configuration) =>
            services.AddDbContext<ImateDbContext>(opts =>
                opts.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        public static void ConfigureExternalServices(this IServiceCollection services)
        {
            services.AddScoped<Imate.API.Business.Interfaces.ExternalServices.IAwsS3StorageService, AwsS3StorageService>();
            // OpenAIService removed - replaced by GeminiService in AIAdapterExtensions
            services.AddScoped<PayOSService>();
        }
        
        public static void ConfigureBackgroundServices(this IServiceCollection services)
        {
            services.AddHostedService<SubscriptionExpirationBackgroundService>();
        }

        public static void ConfigureRepositoryManager(this IServiceCollection services)
        {
            services.AddScoped<IUnitOfWork, UnitOfWork>();
        }
    }
}
