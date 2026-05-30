namespace WebAppTemplate1.Services.Email
{
    /// <summary>
    /// Configuration options for email providers.
    /// </summary>
    public class EmailProviderOptions
    {
        public const string SectionName = "EmailProvider";

        /// <summary>
        /// The primary email provider to use (MicrosoftGraph, AzureCommunicationServices, SendGrid).
        /// </summary>
        public string Provider { get; set; } = "MicrosoftGraph";

        /// <summary>
        /// Fallback provider if primary fails (optional).
        /// </summary>
        public string? FallbackProvider { get; set; }

        /// <summary>
        /// Enable automatic fallback to secondary provider on failure.
        /// </summary>
        public bool EnableFallback { get; set; } = false;

        /// <summary>
        /// Common email configuration.
        /// </summary>
        public string FromAddress { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Microsoft Graph specific configuration.
    /// </summary>
    public class MicrosoftGraphOptions
    {
        public const string SectionName = "MicrosoftGraph";

        public string TenantId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
    }

    /// <summary>
    /// Azure Communication Services specific configuration.
    /// </summary>
    public class AzureCommunicationServicesOptions
    {
        public const string SectionName = "AzureCommunicationServices";

        public string ConnectionString { get; set; } = string.Empty;
    }

    /// <summary>
    /// SendGrid specific configuration.
    /// </summary>
    public class SendGridOptions
    {
        public const string SectionName = "SendGrid";

        public string ApiKey { get; set; } = string.Empty;
    }
}
