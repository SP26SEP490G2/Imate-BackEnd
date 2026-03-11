using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Imate.API.Business.Interfaces;
using Imate.API.Business.Interfaces.ExternalServices;
using Imate.API.Business.Interfaces.UserManagement;
using Imate.API.Business.Interfaces.Classification;
using Imate.API.Business.Interfaces.Payment;
using Imate.API.Business.Services;
using Imate.API.Business.Services.UserManagement;
using Imate.API.Business.Interfaces.Recruiters;
using Imate.API.Business.Interfaces.Staff;
using Imate.API.Business.Services.Recruiters;
using Imate.API.Business.Services.Staff;
using Imate.API.Business.Services.Classification;
using Imate.API.Business.Services.Payment;
using Imate.API.DataAccess.Interfaces;
using Imate.API.DataAccess.Interfaces.Mentors;
using Imate.API.DataAccess.Interfaces.Payment;
using Imate.API.DataAccess.Interfaces.QuestionBank;
using Imate.API.DataAccess.Interfaces.UserManagement;
using Imate.API.DataAccess.Interfaces.Classification;
using Imate.API.DataAccess.Repositories;
using Imate.API.DataAccess.Repositories.Mentors;
using Imate.API.DataAccess.Repositories.Payment;
using Imate.API.DataAccess.Repositories.QuestionBank;
using Imate.API.DataAccess.Repositories.UserManagement;
using Imate.API.DataAccess.Interfaces.Recruiters;
using Imate.API.DataAccess.Repositories.Recruiters;
using Imate.API.DataAccess.Repositories.Classification;
using System.Text;
using System.Reflection;
using Imate.API.Business.Services.ExternalServices;
using Imate.API.Business.Interfaces.Recruiters;
using Imate.API.ExternalServices;
using Amazon.S3;



namespace Imate.API.Infrastructure.Configurations
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddMyServices(this IServiceCollection services, IConfiguration configuration)
        {
            // 1. Cấu hình Options Pattern 
            services.Configure<JwtSettings>(
                configuration.GetSection(JwtSettings.SectionName));
            // 2. Cấu hình JWT Authentication 
            ConfigureJwtAuthentication(services, configuration);
            // 3 Cấu hình Swagger
            ConfigureSwagger(services);
            
            // Register MediatR
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
            
            // 3. Đăng ký các Repository và Service 
            services.AddScoped<IQuestionRepository, QuestionRepository>();
            services.AddScoped<IAccountRepository, DataAccess.Repositories.UserManagement.AccountRepository>();
            services.AddScoped<IMentorRepository, MentorRepository>();

            services.AddScoped<Business.Interfaces.UserManagement.IAccountService, Business.Services.UserManagement.AccountService>();
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            services.AddScoped<IAuditLogService, AuditLogService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
            services.AddScoped<IUserSubscriptionRepository, UserSubscriptionRepository>();
            services.AddScoped<ISubscriptionPackageRepository, SubscriptionPackageRepository>();
            services.AddScoped<ISubscriptionPackageService, SubscriptionPackageService>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IMentorRepository, MentorRepository>();
            services.AddScoped<IRecruiterRepository, RecruiterRepository>();
            services.AddScoped<IRecruiterService, RecruiterService>();
            

            // Classification Services & Repositories
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IPositionService, PositionService>();
            services.AddScoped<IPositionRepository, PositionRepository>();
            services.AddScoped<ISkillService, SkillService>();
            services.AddScoped<ISkillRepository, SkillRepository>();
            services.AddScoped<ICompanyService, CompanyService>();
            services.AddScoped<ICompanyRepository, CompanyRepository>();
            services.AddScoped<IStaffReviewService, StaffReviewService>();

            //Add Merory Cache
            services.AddMemoryCache();
            // Register HttpClient for Resend API
            services.AddHttpClient<Imate.API.Business.Services.ExternalServices.EmailService>();
            services.AddScoped<IEmailService, Imate.API.Business.Services.ExternalServices.EmailService>();

            services.AddScoped<IUserCvRepository, UserCvRepository>();

            // AWS S3 Storage Service
            services.Configure<AwsS3Config>(
                configuration.GetSection(AwsS3Config.ConfigSectionName));

            var awsS3Config = configuration.GetSection(AwsS3Config.ConfigSectionName).Get<AwsS3Config>();
            if (awsS3Config != null)
            {
                services.AddSingleton<IAmazonS3>(sp =>
                {
                    var credentials = new Amazon.Runtime.BasicAWSCredentials(awsS3Config.AccessKey, awsS3Config.SecretKey);
                    var config = new AmazonS3Config
                    {
                        RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(awsS3Config.RegionName)
                    };
                    return new AmazonS3Client(credentials, config);
                });
            }

            services.AddScoped<IAwsS3StorageService, AwsS3StorageService>();


            services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 104857600;
            });
            services.Configure<MailSettings>(configuration.GetSection("MailSettings"));





            return services;
        }

        private static void ConfigureJwtAuthentication(IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();

            if (jwtSettings == null)
            {
                throw new InvalidOperationException($"JwtSettings section '{JwtSettings.SectionName}' not found or is empty.");
            }

            var key = Encoding.UTF8.GetBytes(jwtSettings.Secret);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                // NOTE: Hãy đảm bảo set TRUE khi triển khai lên môi trường Production
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero // Loại bỏ độ trễ mặc định (5 phút)
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // Lấy token từ query string
                        var accessToken = context.Request.Query["access_token"];

                        // Lấy path (ví dụ: /systemNotificationHub)
                        var path = context.HttpContext.Request.Path;

                        // Chỉ thực hiện khi có token VÀ request đi đến Hub của bạn
                        if (!string.IsNullOrEmpty(accessToken) &&
                            path.StartsWithSegments("/api/systemNotificationHub")) // << Tên Hub của bạn
                        {
                            // Gán token này cho context để xác thực
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });
        }

        private static void ConfigureSwagger(IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Nhập 'Bearer' [dấu cách] và sau đó là token của bạn. \r\n\r\nVí dụ: \"Bearer 12345abcdef\""
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });

                // Handle IFormFile for file uploads in Swagger
                options.MapType<IFormFile>(() => new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary"
                });
            });
        }
    }
}
