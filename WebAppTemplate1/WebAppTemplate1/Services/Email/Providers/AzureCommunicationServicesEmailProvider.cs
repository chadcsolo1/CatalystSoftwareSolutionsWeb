using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Options;

namespace WebAppTemplate1.Services.Email.Providers
{
    /// <summary>
    /// Azure Communication Services email provider implementation.
    /// Purpose-built for transactional application emails with better deliverability.
    /// </summary>
    public class AzureCommunicationServicesEmailProvider : IEmailProvider
    {
        private readonly EmailClient _emailClient;
        private readonly ILogger<AzureCommunicationServicesEmailProvider> _logger;
        private readonly EmailProviderOptions _emailOptions;

        public string ProviderName => "AzureCommunicationServices";

        public AzureCommunicationServicesEmailProvider(
            IOptions<EmailProviderOptions> emailOptions,
            IOptions<AzureCommunicationServicesOptions> acsOptions,
            ILogger<AzureCommunicationServicesEmailProvider> logger)
        {
            _emailOptions = emailOptions.Value;
            _logger = logger;

            // Initialize ACS Email client
            _emailClient = new EmailClient(acsOptions.Value.ConnectionString);
        }

        public async Task<EmailResult> SendEmailAsync(EmailMessage message)
        {
            try
            {
                if (!message.IsValid())
                {
                    return EmailResult.Failure("Invalid email message: missing required fields", ProviderName);
                }

                var emailMessage = new Azure.Communication.Email.EmailMessage(
                    senderAddress: _emailOptions.FromAddress,
                    recipientAddress: message.ToEmail,
                    content: new EmailContent(message.Subject)
                    {
                        Html = message.HtmlBody,
                        PlainText = message.TextBody
                    });

                // Add attachments if any
                if (message.Attachments?.Any() == true)
                {
                    foreach (var attachment in message.Attachments)
                    {
                        var acsAttachment = new Azure.Communication.Email.EmailAttachment(
                            attachment.FileName,
                            attachment.ContentType,
                            new BinaryData(attachment.Content));

                        emailMessage.Attachments.Add(acsAttachment);
                    }
                }

                // Send email
                EmailSendOperation emailSendOperation = await _emailClient.SendAsync(
                    WaitUntil.Started,
                    emailMessage);

                _logger.LogInformation("Email sent successfully via {Provider} to {Recipient}. MessageId: {MessageId}",
                    ProviderName, message.ToEmail, emailSendOperation.Id);

                return EmailResult.Success(messageId: emailSendOperation.Id, provider: ProviderName);
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
