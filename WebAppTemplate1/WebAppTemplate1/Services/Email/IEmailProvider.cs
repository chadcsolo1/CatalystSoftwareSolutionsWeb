namespace WebAppTemplate1.Services.Email
{
    /// <summary>
    /// Defines the contract for email provider implementations.
    /// Follows Interface Segregation Principle - focused on email sending only.
    /// </summary>
    public interface IEmailProvider
    {
        /// <summary>
        /// Sends an email message asynchronously.
        /// </summary>
        /// <param name="message">The email message to send.</param>
        /// <returns>A task representing the asynchronous operation, containing the result.</returns>
        Task<EmailResult> SendEmailAsync(EmailMessage message);

        /// <summary>
        /// Gets the name of the email provider.
        /// </summary>
        string ProviderName { get; }
    }
}
