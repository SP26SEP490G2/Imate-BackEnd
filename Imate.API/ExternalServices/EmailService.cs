namespace Imate.API.ExternalServices
{
    public class EmailService
    {
        // Add SMTP email sending logic here
        public async Task SendEmailAsync(string to, string subject, string body)
        {
            await Task.Delay(100);
        }
    }
}
