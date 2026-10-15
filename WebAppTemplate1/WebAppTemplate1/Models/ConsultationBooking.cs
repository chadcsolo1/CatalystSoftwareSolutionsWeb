namespace WebAppTemplate1.Models
{
    public class ConsultationBooking
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime ConsultationDate { get; set; }
        public string ConsultationTime { get; set; } = string.Empty;
        /// <summary>
        /// Client's timezone (e.g., "Eastern Standard Time", "Pacific Standard Time").
        /// Used to convert client-local time to America/New_York for calendar events.
        /// </summary>
        public string? ClientTimeZone { get; set; }
    }
}
