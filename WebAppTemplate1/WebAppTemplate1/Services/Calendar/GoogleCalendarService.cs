using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Options;

namespace WebAppTemplate1.Services.Calendar
{
    /// <summary>
    /// Google Calendar implementation of ICalendarService using a service account.
    /// Expects service account JSON to be provided in configuration (e.g., secrets or environment).
    /// </summary>
    public class GoogleCalendarService : ICalendarService
    {
        private readonly CalendarService _calendarService;
        private readonly ILogger<GoogleCalendarService> _logger;
        private readonly CalendarOptions _calendarOptions;

        private readonly Dictionary<DayOfWeek, (TimeSpan Start, TimeSpan End)> _businessHours = new()
        {
            { DayOfWeek.Monday, (new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0)) },
            { DayOfWeek.Tuesday, (new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0)) },
            { DayOfWeek.Friday, (new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0)) }
        };

        private const int ConsultationDurationMinutes = 30;
        private const int BufferMinutes = 10;

        public GoogleCalendarService(
            IOptions<GoogleCalendarOptions> googleOptions,
            IOptions<CalendarOptions> calendarOptions,
            ILogger<GoogleCalendarService> logger)
        {
            _calendarOptions = calendarOptions.Value;
            _logger = logger;

            var gaOpts = googleOptions.Value;
            if (string.IsNullOrWhiteSpace(gaOpts.ServiceAccountJson))
            {
                throw new InvalidOperationException("Google service account JSON is not configured. Set GoogleCalendar:ServiceAccountJson in configuration or environment secrets.");
            }

            // Create credential from JSON and scope to Calendar
            var credential = GoogleCredential.FromJson(gaOpts.ServiceAccountJson)
                .CreateScoped(CalendarService.Scope.Calendar);

            _calendarService = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = gaOpts.ApplicationName
            });
        }

        public async Task<List<string>> GetAvailableTimeSlotsAsync(DateTime date)
        {
            try
            {
                if (!_businessHours.ContainsKey(date.DayOfWeek))
                {
                    _logger.LogInformation("Date {Date} is not a business day", date.ToShortDateString());
                    return new List<string>();
                }

                var (startTime, endTime) = _businessHours[date.DayOfWeek];

                // Use America/New_York as business timezone
                var tz = "America/New_York";
                var startDateTime = new DateTime(date.Year, date.Month, date.Day, startTime.Hours, startTime.Minutes, 0, DateTimeKind.Unspecified);
                var endDateTime = new DateTime(date.Year, date.Month, date.Day, endTime.Hours, endTime.Minutes, 0, DateTimeKind.Unspecified);

                var req = _calendarService.Events.List(_calendarOptions.CalendarOwnerEmail);
                req.TimeMin = startDateTime;
                req.TimeMax = endDateTime;
                req.TimeZone = tz;
                req.SingleEvents = true;
                req.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

                var events = await req.ExecuteAsync();

                var allSlots = GenerateTimeSlots(startTime, endTime);
                var availableSlots = new List<string>();

                foreach (var slot in allSlots)
                {
                    var slotTimeSpan = ParseTimeSlot(slot);
                    var slotStart = new DateTime(date.Year, date.Month, date.Day, slotTimeSpan.Hours, slotTimeSpan.Minutes, 0, DateTimeKind.Unspecified);
                    var slotEnd = slotStart.AddMinutes(ConsultationDurationMinutes);

                    bool isAvailable = true;

                    if (events?.Items != null)
                    {
                        foreach (var evt in events.Items)
                        {
                            if (evt.Start == null || (evt.Start.DateTime == null && string.IsNullOrEmpty(evt.Start.Date)))
                                continue;

                            DateTime eventStart;
                            DateTime eventEnd;

                            if (evt.Start.DateTime != null && evt.End.DateTime != null)
                            {
                                eventStart = evt.Start.DateTime.Value;
                                eventEnd = evt.End.DateTime.Value;
                            }
                            else
                            {
                                // All-day event - mark full-day as busy
                                eventStart = startDateTime;
                                eventEnd = endDateTime;
                            }

                            bool overlaps = slotStart < eventEnd.AddMinutes(BufferMinutes) && slotEnd.AddMinutes(BufferMinutes) > eventStart;
                            if (overlaps)
                            {
                                isAvailable = false;
                                break;
                            }
                        }
                    }

                    if (isAvailable)
                        availableSlots.Add(slot);
                }

                _logger.LogInformation("Found {Count} available slots for {Date}", availableSlots.Count, date.ToShortDateString());
                return availableSlots;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available time slots for {Date}", date.ToShortDateString());
                throw;
            }
        }

        public async Task<string> CreateConsultationEventAsync(Models.ConsultationBooking booking)
        {
            try
            {
                var tz = "America/New_York";
                // Parse time from booking. Expect format HH:mm AM/PM or HH:mm
                var timePart = booking.ConsultationTime;
                var dt = DateTime.Parse(timePart);
                var startLocal = new DateTime(booking.ConsultationDate.Year, booking.ConsultationDate.Month, booking.ConsultationDate.Day, dt.Hour, dt.Minute, 0, DateTimeKind.Unspecified);
                var endLocal = startLocal.AddMinutes(ConsultationDurationMinutes);

                var newEvent = new Event
                {
                    Summary = $"Consultation: {booking.FullName}",
                    Description = booking.Message,
                    Start = new EventDateTime { DateTime = startLocal, TimeZone = tz },
                    End = new EventDateTime { DateTime = endLocal, TimeZone = tz },
                    Attendees = new List<EventAttendee>
                    {
                        new EventAttendee { Email = booking.Email }
                    }
                };

                var insertReq = _calendarService.Events.Insert(newEvent, _calendarOptions.CalendarOwnerEmail);
                var created = await insertReq.ExecuteAsync();

                _logger.LogInformation("Calendar event created successfully. Event ID: {EventId}", created.Id);
                return created.Id ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create calendar event. Booking will proceed without calendar integration.");
                throw;
            }
        }

        #region Helpers
        private static List<string> GenerateTimeSlots(TimeSpan start, TimeSpan end)
        {
            var slots = new List<string>();
            var current = new TimeSpan(start.Hours, start.Minutes, 0);
            while (current + TimeSpan.FromMinutes(ConsultationDurationMinutes) <= end)
            {
                slots.Add(DateTime.Today.Add(current).ToString("hh:mm tt"));
                current = current.Add(TimeSpan.FromMinutes(ConsultationDurationMinutes));
            }
            return slots;
        }

        private static TimeSpan ParseTimeSlot(string slot)
        {
            return DateTime.Parse(slot).TimeOfDay;
        }
        #endregion
    }
}
