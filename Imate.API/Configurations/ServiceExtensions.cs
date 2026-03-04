using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.DataAccess.Interfaces;
using Imate.API.DataAccess.Repositories;
using Imate.API.ExternalServices;
using Imate.API.BackgroundServices;

using Imate.API.Business.Interfaces;
using Imate.API.Business.Services;

namespace Imate.API.Configurations
{
    public static class ServiceExtensions
    {
        public static void ConfigureServices(this IServiceCollection services)
        {
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IMentorService, MentorService>();
            services.AddScoped<IQuestionService, QuestionService>();
        }

        public static void ConfigureCors(this IServiceCollection services) =>
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder =>
                    builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader());
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
            services.AddScoped<AwsS3StorageService>();
            services.AddScoped<EmailService>();
            services.AddScoped<OpenAIService>();
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
