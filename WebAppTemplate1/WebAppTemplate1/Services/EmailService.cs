using System.Net;
using System.Net.Mail;

namespace WebAppTemplate1.Services
{
    public interface IEmailService
    {
        Task SendConsultationConfirmationAsync(Models.ConsultationBooking booking);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendConsultationConfirmationAsync(Models.ConsultationBooking booking)
        {
            try
            {
                var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
                var fromEmail = _configuration["Email:FromAddress"] ?? "noreply@catalystsoftware.com";
                var fromName = _configuration["Email:FromName"] ?? "Catalyst Software Solutions";
                var username = _configuration["Email:Username"];
                var password = _configuration["Email:Password"];

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(username, password)
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = "Consultation Confirmed - Catalyst Software Solutions",
                    IsBodyHtml = true,
                    Body = GenerateEmailBody(booking)
                };

                mailMessage.To.Add(booking.Email);

                await client.SendMailAsync(mailMessage);

                _logger.LogInformation($"Consultation confirmation email sent to {booking.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send consultation confirmation email to {booking.Email}");
                throw;
            }
        }

        private string GenerateEmailBody(Models.ConsultationBooking booking)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #2d3748; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #FF6B35 0%, #0066CC 100%); padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .header h1 {{ color: white; margin: 0; font-size: 28px; }}
        .content {{ background: #ffffff; padding: 30px; border: 1px solid #e2e8f0; }}
        .details {{ background: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0; }}
        .detail-row {{ display: flex; justify-content: space-between; padding: 10px 0; border-bottom: 1px solid #e2e8f0; }}
        .detail-row:last-child {{ border-bottom: none; }}
        .detail-label {{ font-weight: 600; color: #2d3748; }}
        .detail-value {{ color: #718096; }}
        .footer {{ background: #f8f9fa; padding: 20px; text-align: center; border-radius: 0 0 8px 8px; font-size: 14px; color: #718096; }}
        .button {{ display: inline-block; padding: 12px 24px; background: linear-gradient(135deg, #FF6B35 0%, #0066CC 100%); color: white; text-decoration: none; border-radius: 6px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✓ Consultation Confirmed</h1>
        </div>
        <div class='content'>
            <p>Dear {booking.FullName},</p>
            <p>Thank you for scheduling a consultation with Catalyst Software Solutions. We're excited to discuss how we can help transform your business.</p>

            <div class='details'>
                <h3 style='margin-top: 0;'>Appointment Details</h3>
                <div class='detail-row'>
                    <span class='detail-label'>Date:</span>
                    <span class='detail-value'>{booking.ConsultationDate:dddd, MMMM dd, yyyy}</span>
                </div>
                <div class='detail-row'>
                    <span class='detail-label'>Time:</span>
                    <span class='detail-value'>{booking.ConsultationTime}</span>
                </div>
                <div class='detail-row'>
                    <span class='detail-label'>Duration:</span>
                    <span class='detail-value'>30 minutes</span>
                </div>
                <div class='detail-row'>
                    <span class='detail-label'>Phone:</span>
                    <span class='detail-value'>{booking.Phone}</span>
                </div>
                {(!string.IsNullOrWhiteSpace(booking.Company) ? $@"
                <div class='detail-row'>
                    <span class='detail-label'>Company:</span>
                    <span class='detail-value'>{booking.Company}</span>
                </div>" : "")}
            </div>

            <h3>What's Next?</h3>
            <ul>
                <li>A calendar invite has been sent to your email</li>
                <li>We'll send you a reminder 24 hours before your consultation</li>
                <li>If you need to reschedule, please reply to this email or call us at (555) 123-4567</li>
            </ul>

            <p>We look forward to speaking with you!</p>

            <p style='margin-top: 30px;'>
                Best regards,<br>
                <strong>The Catalyst Software Solutions Team</strong>
            </p>
        </div>
        <div class='footer'>
            <p>Catalyst Software Solutions | info@catalystsoftware.com | (555) 123-4567</p>
            <p>This is an automated confirmation email. Please do not reply directly to this message.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
