namespace WebAppTemplate1.Services.Calendar
{
    public class GoogleCalendarOptions
    {
        public const string SectionName = "GoogleCalendar";

        /// <summary>
        /// The service account JSON contents. In production prefer storing this in a secure secret store
        /// and exposing it through environment variables or managed secrets.
        /// </summary>
        public string ServiceAccountJson { get; set; } = string.Empty;

        /// <summary>
        /// Optional application name for Google API client initialization.
        /// </summary>
        public string ApplicationName { get; set; } = "CatalystSoftwareSolutionsWeb";
    }
}
