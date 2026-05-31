using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace WebAppTemplate1.Services.Email.Providers
{
    /// <summary>
    /// SendGrid email provider implementation.
    /// Provides reliable transactional email delivery with excellent deliverability.
    /// </summary>
    public class SendGridEmailProvider : IEmailProvider
    {
        private readonly ILogger<SendGridEmailProvider> _logger;
        private readonly EmailProviderOptions _emailOptions;
        private readonly SendGridOptions _sendGridOptions;
        private readonly SendGridClient _client;

        public string ProviderName => "SendGrid";

        public SendGridEmailProvider(
            IOptions<EmailProviderOptions> emailOptions,
            IOptions<SendGridOptions> sendGridOptions,
            ILogger<SendGridEmailProvider> logger)
        {
            _emailOptions = emailOptions.Value;
            _sendGridOptions = sendGridOptions.Value;
            _logger = logger;

            if (string.IsNullOrEmpty(_sendGridOptions.ApiKey))
            {
                throw new InvalidOperationException("SendGrid API key is not configured.");
            }

            _client = new SendGridClient(_sendGridOptions.ApiKey);
        }

        public async Task<EmailResult> SendEmailAsync(EmailMessage message)
        {
            try
            {
                if (!message.IsValid())
                {
                    return EmailResult.Failure("Invalid email message", ProviderName);
                }

                _logger.LogInformation("Sending email via {Provider} to {Recipient}", 
                    ProviderName, message.ToEmail);

                // Create from and to email addresses
                var from = new EmailAddress(_emailOptions.FromAddress, _emailOptions.FromName);
                var to = new EmailAddress(message.ToEmail, message.ToName);

                // Create the email message
                var sendGridMessage = MailHelper.CreateSingleEmail(
                    from, 
                    to, 
                    message.Subject, 
                    message.TextBody ?? message.HtmlBody, // Plain text fallback
                    message.HtmlBody);

                // Add attachments if any
                if (message.Attachments?.Any() == true)
                {
                    foreach (var attachment in message.Attachments)
                    {
                        var base64Content = Convert.ToBase64String(attachment.Content);
                        sendGridMessage.AddAttachment(
                            attachment.FileName,
                            base64Content,
                            attachment.ContentType);
                    }
                }

                // Add custom headers if any
                if (message.Headers?.Any() == true)
                {
                    foreach (var header in message.Headers)
                    {
                        sendGridMessage.AddHeader(header.Key, header.Value);
                    }
                }

                // Send the email
                var response = await _client.SendEmailAsync(sendGridMessage);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Email sent successfully via {Provider} to {Recipient}. StatusCode: {StatusCode}",
                        ProviderName, message.ToEmail, response.StatusCode);

                    // Try to get message ID from headers
                    string? messageId = null;
                    if (response.Headers.TryGetValues("X-Message-Id", out var messageIds))
                    {
                        messageId = messageIds.FirstOrDefault();
                    }

                    return EmailResult.Success(messageId: messageId, provider: ProviderName);
                }
                else
                {
                    var errorBody = await response.Body.ReadAsStringAsync();
                    _logger.LogError("Failed to send email via {Provider} to {Recipient}. StatusCode: {StatusCode}, Body: {Body}",
                        ProviderName, message.ToEmail, response.StatusCode, errorBody);

                    return EmailResult.Failure(
                        $"SendGrid returned status code {response.StatusCode}: {errorBody}",
                        ProviderName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email via {Provider} to {Recipient}",
                    ProviderName, message.ToEmail);

                return EmailResult.Failure(ex.Message, ProviderName);
            }
        }
    }
}
