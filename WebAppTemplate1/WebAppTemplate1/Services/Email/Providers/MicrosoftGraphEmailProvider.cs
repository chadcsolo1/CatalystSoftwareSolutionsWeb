using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Extensions.Options;

namespace WebAppTemplate1.Services.Email.Providers
{
    /// <summary>
    /// Microsoft Graph email provider implementation.
    /// Uses Microsoft 365 mailbox to send emails via Graph API.
    /// </summary>
    public class MicrosoftGraphEmailProvider : IEmailProvider
    {
        private readonly GraphServiceClient _graphClient;
        private readonly ILogger<MicrosoftGraphEmailProvider> _logger;
        private readonly EmailProviderOptions _emailOptions;
        private readonly MicrosoftGraphOptions _graphOptions;

        public string ProviderName => "MicrosoftGraph";

        public MicrosoftGraphEmailProvider(
            IOptions<EmailProviderOptions> emailOptions,
            IOptions<MicrosoftGraphOptions> graphOptions,
            ILogger<MicrosoftGraphEmailProvider> logger)
        {
            _emailOptions = emailOptions.Value;
            _graphOptions = graphOptions.Value;
            _logger = logger;

            // Initialize Graph client with client credentials
            var clientSecretCredential = new ClientSecretCredential(
                _graphOptions.TenantId,
                _graphOptions.ClientId,
                _graphOptions.ClientSecret);

            _graphClient = new GraphServiceClient(clientSecretCredential);
        }

        public async Task<EmailResult> SendEmailAsync(EmailMessage message)
        {
            try
            {
                if (!message.IsValid())
                {
                    return EmailResult.Failure("Invalid email message: missing required fields", ProviderName);
                }

                var graphMessage = new Message
                {
                    Subject = message.Subject,
                    Body = new ItemBody
                    {
                        ContentType = BodyType.Html,
                        Content = message.HtmlBody
                    },
                    ToRecipients = new List<Recipient>
                    {
                        new Recipient
                        {
                            EmailAddress = new EmailAddress
                            {
                                Address = message.ToEmail,
                                Name = message.ToName
                            }
                        }
                    }
                };

                // Add attachments if any
                if (message.Attachments?.Any() == true)
                {
                    graphMessage.Attachments = message.Attachments.Select(a => new FileAttachment
                    {
                        Name = a.FileName,
                        ContentBytes = a.Content,
                        ContentType = a.ContentType
                    }).ToList<Attachment>();
                }

                // Send email using the configured from address
                await _graphClient.Users[_emailOptions.FromAddress]
                    .SendMail
                    .PostAsync(new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
                    {
                        Message = graphMessage,
                        SaveToSentItems = true
                    });

                _logger.LogInformation("Email sent successfully via {Provider} to {Recipient}",
                    ProviderName, message.ToEmail);

                return EmailResult.Success(provider: ProviderName);
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
