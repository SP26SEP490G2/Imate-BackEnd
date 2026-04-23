using Imate.API.Business.Interfaces.ExternalServices;
using Imate.API.Business.Interfaces.Notification;
using Imate.API.Presentation.SignalR.Events.Staff;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Imate.API.Business.Handlers
{
    public class ReviewNotificationHandler : 
        INotificationHandler<MentorReviewCompletedEvent>,
        INotificationHandler<RecruiterReviewCompletedEvent>
    {
        private readonly IEmailService _emailService;
        private readonly ISystemNotificationService _systemNotificationService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ReviewNotificationHandler> _logger;
        private readonly string _frontendBaseUrl;

        public ReviewNotificationHandler(
            IEmailService emailService, 
            ISystemNotificationService systemNotificationService,
            IConfiguration configuration,
            ILogger<ReviewNotificationHandler> logger)
        {
            _emailService = emailService;
            _systemNotificationService = systemNotificationService;
            _configuration = configuration;
            _logger = logger;
            _frontendBaseUrl = _configuration["FrontendSettings:BaseUrl"] ?? "http://localhost:3000";
        }

        public async Task Handle(MentorReviewCompletedEvent notification, CancellationToken cancellationToken)
        {
            var account = notification.Account;
            var isApproved = notification.IsApproved;
            var note = notification.Note;

            // 1. Send Email
            try 
            {
                var subject = isApproved ? "Hồ sơ Mentor của bạn đã được phê duyệt! - Imate" : "Thông tin về hồ sơ Mentor của bạn - Imate";
                var body = GenerateReviewEmailTemplate(account.FullName, "Mentor", isApproved, note);
                await _emailService.SendEmailAsync(account.Email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send review completion email to {Email}", account.Email);
            }

            // 2. In-app Notification
            var message = isApproved 
                ? "Chúc mừng! Hồ sơ Mentor của bạn đã được phê duyệt. Bạn có thể bắt đầu thiết lập lịch hẹn ngay bây giờ." 
                : $"Rất tiếc, hồ sơ Mentor của bạn chưa được phê duyệt. Lý do: {note}";
            var link = isApproved ? "/mentor/dashboard" : "/profile/mentor";
            await _systemNotificationService.CreateAndSendNotificationAsync(account.Id, message, link);
        }

        public async Task Handle(RecruiterReviewCompletedEvent notification, CancellationToken cancellationToken)
        {
            var account = notification.Account;
            var isApproved = notification.IsApproved;
            var note = notification.Note;

            // 1. Send Email
            try 
            {
                var subject = isApproved ? "Hồ sơ Nhà tuyển dụng của bạn đã được phê duyệt! - Imate" : "Thông tin về hồ sơ Nhà tuyển dụng của bạn - Imate";
                var body = GenerateReviewEmailTemplate(account.FullName, "Nhà tuyển dụng", isApproved, note);
                await _emailService.SendEmailAsync(account.Email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send review completion email to {Email}", account.Email);
            }

            // 2. In-app Notification
            var message = isApproved 
                ? "Chúc mừng! Hồ sơ Nhà tuyển dụng của bạn đã được phê duyệt. Bạn có thể bắt đầu đăng tin tuyển dụng ngay bây giờ." 
                : $"Rất tiếc, hồ sơ Nhà tuyển dụng của bạn chưa được phê duyệt. Lý do: {note}";
            var link = isApproved ? "/recruiter/dashboard" : "/profile/recruiter";
            await _systemNotificationService.CreateAndSendNotificationAsync(account.Id, message, link);
        }

        private string GenerateReviewEmailTemplate(string fullName, string roleName, bool isApproved, string? note)
        {
            var logoUrl = $"{_frontendBaseUrl}/src/assets/images/logo.png";
            var statusText = isApproved ? "Đã được phê duyệt" : "Chưa được phê duyệt";
            var statusColor = isApproved ? "#4caf50" : "#ff6b6b";
            var backgroundColor = isApproved ? "#f1f8f4" : "#fff5f5";
            var borderColor = isApproved ? "#4caf50" : "#ff6b6b";

            var contentHtml = isApproved 
                ? $@"<p style='color: #666666; font-size: 16px; line-height: 1.6; margin: 0 0 20px;'>
                        Chúc mừng <strong>{fullName}</strong>,
                    </p>
                    <p style='color: #666666; font-size: 16px; line-height: 1.6; margin: 0 0 20px;'>
                        Hồ sơ đăng ký làm <strong>{roleName}</strong> của bạn trên hệ thống Imate đã được đội ngũ chúng tôi xem xét và <strong>phê duyệt thành công</strong>.
                    </p>
                    <p style='color: #666666; font-size: 16px; line-height: 1.6; margin: 0 0 20px;'>
                        Bây giờ bạn có thể truy cập vào Dashboard để bắt đầu sử dụng đầy đủ các tính năng dành cho {roleName}.
                    </p>"
                : $@"<p style='color: #666666; font-size: 16px; line-height: 1.6; margin: 0 0 20px;'>
                        Xin chào <strong>{fullName}</strong>,
                    </p>
                    <p style='color: #666666; font-size: 16px; line-height: 1.6; margin: 0 0 20px;'>
                        Cảm ơn bạn đã quan tâm và gửi hồ sơ đăng ký làm <strong>{roleName}</strong> trên Imate. Sau khi xem xét kỹ lưỡng, chúng tôi rất tiếc phải thông báo rằng hồ sơ của bạn chưa đáp ứng được các tiêu chí hiện tại của hệ thống.
                    </p>
                    <div style='color: #333333; font-size: 14px; line-height: 1.6; margin: 20px 0; padding: 15px; background-color: {backgroundColor}; border-left: 4px solid {borderColor}; border-radius: 4px;'>
                        <strong>Lý do từ chối:</strong> {note ?? "Hồ sơ chưa đạt yêu cầu chuyên môn."}
                    </div>
                    <p style='color: #666666; font-size: 16px; line-height: 1.6; margin: 0 0 20px;'>
                        Bạn có thể cập nhật lại hồ sơ của mình và gửi yêu cầu phê duyệt mới bất cứ lúc nào.
                    </p>";

            var actionLink = isApproved 
                ? $"{_frontendBaseUrl}/login" 
                : (roleName == "Mentor" ? $"{_frontendBaseUrl}/profile/mentor" : $"{_frontendBaseUrl}/profile/recruiter");
            
            var actionButtonText = isApproved ? "Đăng nhập ngay" : "Cập nhật hồ sơ";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Kết quả duyệt hồ sơ - Imate</title>
</head>
<body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f5f5f5;'>
    <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f5f5f5; padding: 40px 0;'>
        <tr>
            <td align='center'>
                <table width='600' cellpadding='0' cellspacing='0' style='background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                    <!-- Header with Logo -->
                    <tr>
                        <td align='center' style='padding: 40px 20px 20px;'>
                            <img src='{logoUrl}' alt='Imate Logo' style='max-width: 150px; height: auto;' />
                        </td>
                    </tr>
                    <!-- Content -->
                    <tr>
                        <td style='padding: 20px 40px;'>
                            <h2 style='color: #333333; margin: 0 0 20px; font-size: 24px; text-align: center;'>Kết quả duyệt hồ sơ</h2>
                            <div style='text-align: center; margin-bottom: 30px;'>
                                <span style='display: inline-block; padding: 8px 16px; background-color: {backgroundColor}; color: {statusColor}; font-weight: bold; border-radius: 20px; border: 1px solid {borderColor};'>
                                    {statusText}
                                </span>
                            </div>
                            {contentHtml}
                            <table width='100%' cellpadding='0' cellspacing='0'>
                                <tr>
                                    <td align='center' style='padding: 30px 0;'>
                                        <a href='{actionLink}' style='display: inline-block; padding: 14px 32px; background-color: #5d5fef; color: #ffffff; text-decoration: none; border-radius: 6px; font-size: 16px; font-weight: 600;'>{actionButtonText}</a>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style='padding: 30px 40px; background-color: #f9f9f9; border-top: 1px solid #eeeeee;'>
                            <p style='color: #999999; font-size: 12px; line-height: 1.6; margin: 0; text-align: center;'>
                                Cảm ơn bạn đã đồng hành cùng cộng đồng Imate.<br/>
                                <strong style='color: #333333;'>Đội ngũ Imate</strong>
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }
    }
}
