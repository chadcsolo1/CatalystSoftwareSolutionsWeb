namespace WebAppTemplate1.Services.Calendar
{
    /// <summary>
    /// Service for managing calendar operations including availability and event creation.
    /// </summary>
    public interface ICalendarService
    {
        /// <summary>
        /// Gets available consultation time slots for a specific date.
        /// </summary>
        /// <param name="date">The date to check availability</param>
        /// <returns>List of available time slots in HH:mm format</returns>
        Task<List<string>> GetAvailableTimeSlotsAsync(DateTime date);

        /// <summary>
        /// Creates a consultation event in the calendar.
        /// </summary>
        /// <param name="booking">The consultation booking details</param>
        /// <returns>The created event ID</returns>
        Task<string> CreateConsultationEventAsync(Models.ConsultationBooking booking);
    }
}
