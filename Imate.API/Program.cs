using Imate.API.Configurations;
using Imate.API.Infrastructure.Configurations;
using Imate.API.Middleware;
using Microsoft.Extensions.Configuration;
using PayOS;
using System.Text;

// Fix Windows console encoding for Vietnamese characters
Console.OutputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

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
// Middleware
builder.Services.AddTransient<GlobalExceptionMiddleware>();

builder.Services.AddControllers()
    .AddApplicationPart(typeof(Imate.AI.Module.Controllers.CvAnalysisController).Assembly);
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
