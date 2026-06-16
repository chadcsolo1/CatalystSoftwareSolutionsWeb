using WebAppTemplate1.Services.Email;

namespace WebAppTemplate1.Services
{
    /// <summary>
    /// Email service interface for application-specific email operations.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends a consultation confirmation email to the client.
        /// </summary>
        Task SendConsultationConfirmationAsync(Models.ConsultationBooking booking);

        /// <summary>
        /// Sends a consultation notification email to the business owner.
        /// </summary>
        Task SendConsultationNotificationAsync(Models.ConsultationBooking booking);

        /// <summary>
        /// Sends a generic email message.
        /// </summary>
        Task<EmailResult> SendEmailAsync(EmailMessage message);
    }
}
