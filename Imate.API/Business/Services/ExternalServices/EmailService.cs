using Microsoft.Extensions.Options;
using Imate.API.Business.Interfaces.ExternalServices;
using Imate.API.Infrastructure.Configurations;
using System.Net.Http.Json;

namespace Imate.API.Business.Services.ExternalServices
{
    public class EmailService : IEmailService
    {
        private readonly MailSettings _mailSettings;
        private readonly IHttpClientFactory _httpClientFactory;
        private const string ResendApiUrl = "https://api.resend.com/emails";

        public EmailService(IOptions<MailSettings> mailSettingsOptions, IHttpClientFactory httpClientFactory)
        {
            _mailSettings = mailSettingsOptions.Value;
            _httpClientFactory = httpClientFactory;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                
                // Set up Resend API authentication
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_mailSettings.ApiKey}");

                var requestBody = new
                {
                    from = $"{_mailSettings.DisplayName} <{_mailSettings.FromEmail}>",
                    to = new[] { toEmail },
                    subject = subject,
                    html = body
                };

                var response = await httpClient.PostAsJsonAsync(ResendApiUrl, requestBody);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"Resend API error: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadFromJsonAsync<ResendResponse>();
                
                if (result?.Id == null)
                {
                    throw new InvalidOperationException("Resend API returned invalid response");
                }
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Không thể kết nối đến Resend API: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Không thể gửi email: {ex.Message}", ex);
            }
        }

        private class ResendResponse
        {
            public string? Id { get; set; }
        }
    }
}
