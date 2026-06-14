namespace WebAppTemplate1.Services.Calendar
{
    /// <summary>
    /// Configuration options for calendar service.
    /// </summary>
    public class CalendarOptions
    {
        public const string SectionName = "Calendar";

        /// <summary>
        /// Email address of the calendar owner (business calendar).
        /// </summary>
        public string CalendarOwnerEmail { get; set; } = string.Empty;
    }
}
