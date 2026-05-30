using Microsoft.Extensions.Options;

namespace WebAppTemplate1.Services.Email.Providers
{
    /// <summary>
    /// SendGrid email provider implementation.
    /// To use this provider, install the SendGrid NuGet package:
    /// dotnet add package SendGrid
    /// </summary>
    public class SendGridEmailProvider : IEmailProvider
    {
        private readonly ILogger<SendGridEmailProvider> _logger;
        private readonly EmailProviderOptions _emailOptions;
        private readonly SendGridOptions _sendGridOptions;

        public string ProviderName => "SendGrid";

        public SendGridEmailProvider(
            IOptions<EmailProviderOptions> emailOptions,
            IOptions<SendGridOptions> sendGridOptions,
            ILogger<SendGridEmailProvider> logger)
        {
            _emailOptions = emailOptions.Value;
            _sendGridOptions = sendGridOptions.Value;
            _logger = logger;
        }

        public Task<EmailResult> SendEmailAsync(EmailMessage message)
        {
            // TODO: Implement SendGrid when package is installed
            // Example implementation:
            // var client = new SendGridClient(_sendGridOptions.ApiKey);
            // var from = new EmailAddress(_emailOptions.FromAddress, _emailOptions.FromName);
            // var to = new EmailAddress(message.ToEmail, message.ToName);
            // var msg = MailHelper.CreateSingleEmail(from, to, message.Subject, message.TextBody, message.HtmlBody);
            // var response = await client.SendEmailAsync(msg);

            _logger.LogWarning("SendGrid provider is not yet implemented. Install SendGrid NuGet package first.");
            return Task.FromResult(EmailResult.Failure(
                "SendGrid provider not implemented. Install SendGrid package and uncomment implementation.",
                ProviderName));
        }
    }
}
