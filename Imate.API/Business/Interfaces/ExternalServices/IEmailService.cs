    namespace Imate.API.Business.Interfaces.ExternalServices
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}
