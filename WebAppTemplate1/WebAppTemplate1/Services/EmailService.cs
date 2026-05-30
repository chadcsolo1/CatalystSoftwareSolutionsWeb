using WebAppTemplate1.Services.Email;

namespace WebAppTemplate1.Services
{
    /// <summary>
    /// Email service interface for application-specific email operations.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends a consultation confirmation email.
        /// </summary>
        Task SendConsultationConfirmationAsync(Models.ConsultationBooking booking);

        /// <summary>
        /// Sends a generic email message.
        /// </summary>
        Task<EmailResult> SendEmailAsync(EmailMessage message);
    }
}
