using Imate.API.Business.Services.Payment;
using Imate.API.Business.Services.Recruiters;
using Imate.API.Configurations;
using Imate.API.Infrastructure.Configurations;
using Imate.API.Middleware;
using Imate.API.Presentation.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PayOS;
using System.Text;

// Fix Windows console encoding for Vietnamese characters
Console.OutputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("comment-moderation.json", optional: true, reloadOnChange: true);
var configuration = builder.Configuration;

// Add services to the container.
builder.Services.ConfigureCors();
builder.Services.ConfigureIISIntegration();
builder.Services.ConfigureSqlContext(builder.Configuration);
builder.Services.ConfigureRepositoryManager();
builder.Services.ConfigureServices();
builder.Services.ConfigureExternalServices();
builder.Services.ConfigureBackgroundServices();
builder.Services.RegisterAIAdapters();
builder.Services.AddScoped<PayOSClient>();
builder.Services.AddFirebaseAdmin();
builder.Services.AddMyServices(builder.Configuration);
builder.Services.AddSignalR();

builder.Services.AddHostedService<AutoCloseJobServices>();
builder.Services.AddHostedService<TransactionTimeoutService>();
builder.Services.AddHostedService<SubscriptionExpirationService>();
builder.Services.AddHostedService<Imate.API.Business.Services.Mentors.AutoCompleteBookingService>();
// Middleware
builder.Services.AddTransient<GlobalExceptionMiddleware>();

builder.Services.AddControllers()
    .AddApplicationPart(typeof(Imate.AI.Module.API.Controllers.CvAnalysisController).Assembly);
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionMiddleware>();

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseCors("CorsPolicy");
app.MapHub<SystemNotificationHub>("/api/systemNotificationHub");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
