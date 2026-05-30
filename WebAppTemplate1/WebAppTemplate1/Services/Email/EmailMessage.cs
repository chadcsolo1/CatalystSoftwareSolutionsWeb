namespace WebAppTemplate1.Services.Email
{
    /// <summary>
    /// Represents an email message with all necessary components.
    /// Encapsulates email data and follows Single Responsibility Principle.
    /// </summary>
    public class EmailMessage
    {
        public string ToEmail { get; set; } = string.Empty;
        public string ToName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string HtmlBody { get; set; } = string.Empty;
        public string? TextBody { get; set; }
        public List<EmailAttachment>? Attachments { get; set; }
        public Dictionary<string, string>? Headers { get; set; }

        /// <summary>
        /// Validates that the email message has all required fields.
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(ToEmail) &&
                   !string.IsNullOrWhiteSpace(Subject) &&
                   !string.IsNullOrWhiteSpace(HtmlBody);
        }
    }

    /// <summary>
    /// Represents an email attachment.
    /// </summary>
    public class EmailAttachment
    {
        public string FileName { get; set; } = string.Empty;
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = "application/octet-stream";
    }
}
