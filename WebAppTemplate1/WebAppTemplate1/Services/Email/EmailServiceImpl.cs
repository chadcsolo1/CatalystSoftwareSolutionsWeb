using Microsoft.Extensions.Options;

namespace WebAppTemplate1.Services.Email
{
    /// <summary>
    /// Main email service implementation that orchestrates email sending through various providers.
    /// Supports fallback to secondary provider on failure.
    /// Follows Single Responsibility and Dependency Inversion principles.
    /// </summary>
    public class EmailServiceImpl : Services.IEmailService
    {
        private readonly IEmailProviderFactory _providerFactory;
        private readonly EmailProviderOptions _options;
        private readonly ILogger<EmailServiceImpl> _logger;

        public EmailServiceImpl(
            IEmailProviderFactory providerFactory,
            IOptions<EmailProviderOptions> options,
            ILogger<EmailServiceImpl> logger)
        {
            _providerFactory = providerFactory;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<EmailResult> SendEmailAsync(EmailMessage message)
        {
            // Try primary provider
            var primaryProvider = _providerFactory.GetProvider();
            var result = await primaryProvider.SendEmailAsync(message);

            if (result.IsSuccess)
            {
                return result;
            }

            // Try fallback if enabled and configured
            if (_options.EnableFallback && !string.IsNullOrWhiteSpace(_options.FallbackProvider))
            {
                _logger.LogWarning(
                    "Primary provider {Primary} failed. Attempting fallback to {Fallback}",
                    _options.Provider,
                    _options.FallbackProvider);

                try
                {
                    var fallbackProvider = _providerFactory.GetProvider(_options.FallbackProvider);
                    var fallbackResult = await fallbackProvider.SendEmailAsync(message);

                    if (fallbackResult.IsSuccess)
                    {
                        _logger.LogInformation("Email sent successfully using fallback provider {Provider}",
                            _options.FallbackProvider);
                    }

                    return fallbackResult;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fallback provider {Provider} also failed",
                        _options.FallbackProvider);
                    return EmailResult.Failure(
                        $"Both primary and fallback providers failed. Last error: {ex.Message}",
                        _options.FallbackProvider);
                }
            }

            return result;
        }

        public async Task SendConsultationConfirmationAsync(Models.ConsultationBooking booking)
        {
            var message = new EmailMessage
            {
                ToEmail = booking.Email,
                ToName = booking.FullName,
                Subject = "Consultation Confirmed - Catalyst Software Solutions",
                HtmlBody = GenerateConsultationEmailBody(booking)
            };

            var result = await SendEmailAsync(message);

            if (!result.IsSuccess)
            {
                _logger.LogError("Failed to send consultation confirmation email: {Error}", result.ErrorMessage);
                throw new InvalidOperationException($"Failed to send email: {result.ErrorMessage}");
            }

            _logger.LogInformation("Consultation confirmation email sent to {Email} via {Provider}",
                booking.Email, result.ProviderUsed);
        }

        private string GenerateConsultationEmailBody(Models.ConsultationBooking booking)
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
