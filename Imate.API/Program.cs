using Imate.API.Configurations;
using Imate.API.Business.Interfaces;
using Imate.API.Business.Services;
using Imate.API.Middleware;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.ConfigureCors();
builder.Services.ConfigureIISIntegration();
builder.Services.ConfigureSqlContext(builder.Configuration);
builder.Services.ConfigureExternalServices();
builder.Services.ConfigureBackgroundServices();
builder.Services.RegisterAIAdapters();
builder.Services.ConfigureRepositoryManager();

// Business layer services
builder.Services.AddScoped<IAccountService, AccountService>();

// Middleware
builder.Services.AddTransient<GlobalExceptionMiddleware>();

builder.Services.AddControllers();
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

app.UseHttpsRedirection();

app.UseCors("CorsPolicy");

app.UseAuthorization();

app.MapControllers();

app.Run();app.MapControllers();

app.Run();
