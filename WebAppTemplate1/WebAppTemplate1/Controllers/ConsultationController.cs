using Microsoft.AspNetCore.Mvc;
using WebAppTemplate1.Models;
using WebAppTemplate1.Services;
using WebAppTemplate1.Services.Calendar;

namespace WebAppTemplate1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConsultationController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ICalendarService _calendarService;
        private readonly ILogger<ConsultationController> _logger;

        public ConsultationController(
            IEmailService emailService, 
            ICalendarService calendarService,
            ILogger<ConsultationController> logger)
        {
            _emailService = emailService;
            _calendarService = calendarService;
            _logger = logger;
        }

        [HttpGet("available-times")]
        public async Task<IActionResult> GetAvailableTimes([FromQuery] string date)
        {
            try
            {
                if (!DateTime.TryParse(date, out DateTime consultationDate))
                {
                    return BadRequest(new { success = false, message = "Invalid date format" });
                }

                try
                {
                    var availableSlots = await _calendarService.GetAvailableTimeSlotsAsync(consultationDate);
                    return Ok(new { success = true, availableSlots, calendarIntegrated = true });
                }
                catch (Microsoft.Graph.Models.ODataErrors.ODataError graphEx) when (graphEx.Message.Contains("Access is denied"))
                {
                    // Calendar permissions not configured - return all possible time slots
                    _logger.LogWarning("Calendar API access denied. Returning default time slots. " +
                        "To enable calendar integration, add Calendars.Read and Calendars.ReadWrite permissions to the Azure AD app.");

                    var defaultSlots = GetDefaultTimeSlots(consultationDate);
                    return Ok(new { success = true, availableSlots = defaultSlots, calendarIntegrated = false });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available time slots for {Date}", date);
                return StatusCode(500, new { success = false, message = "Failed to retrieve available times." });
            }
        }

        private List<string> GetDefaultTimeSlots(DateTime date)
        {
            // Only return slots for business days (Mon, Tue, Fri)
            if (date.DayOfWeek != DayOfWeek.Monday && 
                date.DayOfWeek != DayOfWeek.Tuesday && 
                date.DayOfWeek != DayOfWeek.Friday)
            {
                return new List<string>();
            }

            // Generate default 30-minute slots from 8 AM to 5 PM with 10-minute buffer
            var slots = new List<string>();
            var current = new TimeSpan(8, 0, 0);
            var end = new TimeSpan(17, 0, 0);

            while (current.Add(TimeSpan.FromMinutes(30)) <= end)
            {
                var hour = current.Hours;
                var ampm = hour >= 12 ? "PM" : "AM";
                if (hour > 12) hour -= 12;
                if (hour == 0) hour = 12;

                slots.Add($"{hour:D2}:{current.Minutes:D2} {ampm}");
                current = current.Add(TimeSpan.FromMinutes(40)); // 30 min consultation + 10 min buffer
            }

            return slots;
        }

        [HttpPost("book")]
        public async Task<IActionResult> BookConsultation([FromBody] ConsultationBooking booking)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                string? eventId = null;

                // Try to create calendar event (optional - graceful degradation)
                try
                {
                    eventId = await _calendarService.CreateConsultationEventAsync(booking);
                    _logger.LogInformation("Calendar event created successfully. Event ID: {EventId}", eventId);
                }
                catch (Exception calendarEx)
                {
                    // Log the calendar error but don't fail the entire booking
                    _logger.LogWarning(calendarEx, "Failed to create calendar event. Booking will proceed without calendar integration. " +
                        "To enable calendar features, ensure the Azure AD app has Calendars.ReadWrite permissions.");
                }

                // Send confirmation email to client
                await _emailService.SendConsultationConfirmationAsync(booking);

                // Send notification email to business owner
                await _emailService.SendConsultationNotificationAsync(booking);

                _logger.LogInformation("Consultation booked for {Name} on {Date:yyyy-MM-dd} at {Time}", 
                    booking.FullName, booking.ConsultationDate, booking.ConsultationTime);

                return Ok(new { 
                    success = true, 
                    message = "Consultation booked successfully", 
                    eventId,
                    calendarIntegrated = !string.IsNullOrEmpty(eventId)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error booking consultation");
                return StatusCode(500, new { success = false, message = "Failed to book consultation. Please try again." });
            }
        }
    }
}
