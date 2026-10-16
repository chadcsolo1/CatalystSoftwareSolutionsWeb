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

        // Default business hours: Monday through Friday, 8:00 - 17:00
        // Update here or make configurable if you need different hours per day.
        private readonly Dictionary<DayOfWeek, (TimeSpan Start, TimeSpan End)> _businessHours = new()
        {
            { DayOfWeek.Monday, (new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0)) },
            { DayOfWeek.Tuesday, (new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0)) },
            { DayOfWeek.Wednesday, (new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0)) },
            { DayOfWeek.Thursday, (new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0)) },
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

            // Create credential from JSON, scope to Calendar, and impersonate the calendar owner.
            // Domain-wide delegation must be enabled for the service account in Google Workspace
            // and the service account must be authorized to impersonate the target user.
            if (string.IsNullOrWhiteSpace(_calendarOptions.CalendarOwnerEmail))
            {
                throw new InvalidOperationException("CalendarOwnerEmail must be configured for impersonation (Calendar:CalendarOwnerEmail).");
            }

            var credential = GoogleCredential.FromJson(gaOpts.ServiceAccountJson)
                .CreateScoped(CalendarService.Scope.Calendar)
                .CreateWithUser(_calendarOptions.CalendarOwnerEmail);

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

                // Create a datetime in the user's local time first
                var startLocal = new DateTime(booking.ConsultationDate.Year, booking.ConsultationDate.Month, booking.ConsultationDate.Day, dt.Hour, dt.Minute, 0, DateTimeKind.Unspecified);
                var endLocal = startLocal.AddMinutes(ConsultationDurationMinutes);

                // Convert from browser's local time (assumed to be user's timezone) to America/New_York
                // If booking.ClientTimeZone is provided, use it; otherwise assume the times are already in America/New_York
                if (!string.IsNullOrEmpty(booking.ClientTimeZone))
                {
                    try
                    {
                        // JavaScript sends IANA timezone names (e.g., "America/New_York")
                        // but .NET Windows expects Windows timezone names (e.g., "Eastern Standard Time")
                        // Try IANA name first, then convert to Windows name
                        string windowsTimezoneName = ConvertIanaToWindowsTimezone(booking.ClientTimeZone);

                        // Get the client's timezone
                        var clientTz = TimeZoneInfo.FindSystemTimeZoneById(windowsTimezoneName);
                        var clientTime = new DateTimeOffset(startLocal, clientTz.GetUtcOffset(startLocal));

                        // Convert to UTC, then to America/New_York
                        var nyTz = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                        var utcTime = clientTime.ToUniversalTime();
                        var nyTime = TimeZoneInfo.ConvertTime(utcTime.DateTime, TimeZoneInfo.Utc, nyTz);

                        startLocal = new DateTime(nyTime.Year, nyTime.Month, nyTime.Day, nyTime.Hour, nyTime.Minute, 0, DateTimeKind.Unspecified);
                        endLocal = startLocal.AddMinutes(ConsultationDurationMinutes);

                        _logger.LogInformation("Timezone conversion successful. Client TZ: {ClientTz} ({WindowsTz}) -> NY Time", 
                            booking.ClientTimeZone, windowsTimezoneName);
                    }
                    catch (Exception ex)
                    {
                        // If timezone conversion fails, log warning and use time as-is
                        _logger.LogWarning(ex, "Failed to convert timezone {ClientTimezone}. Using time as-is.", booking.ClientTimeZone);
                    }
                }

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

                _logger.LogInformation("Calendar event created successfully. Event ID: {EventId}, Organizer: {Organizer}, Creator: {Creator}, Link: {Link}",
                    created.Id, created?.Organizer?.Email, created?.Creator?.Email, created?.HtmlLink);
                return created.Id ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create calendar event. Booking will proceed without calendar integration.");
                throw;
            }
        }

        /// <summary>
        /// Convert IANA timezone name to Windows timezone name.
        /// IANA names (e.g., "America/New_York") need to be converted to Windows names (e.g., "Eastern Standard Time")
        /// </summary>
        private string ConvertIanaToWindowsTimezone(string ianaTimezoneName)
        {
            // Mapping of common IANA timezone names to Windows timezone names
            var ianaToWindowsMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // North America - Eastern
                { "America/New_York", "Eastern Standard Time" },
                { "America/Detroit", "Eastern Standard Time" },
                { "America/Toronto", "Eastern Standard Time" },
                { "America/Indianapolis", "Eastern Standard Time" },
                { "America/Chicago", "Central Standard Time" },
                { "America/Denver", "Mountain Standard Time" },
                { "America/Phoenix", "US Mountain Standard Time" },
                { "America/Los_Angeles", "Pacific Standard Time" },
                { "America/Anchorage", "Alaskan Standard Time" },
                { "Pacific/Honolulu", "Hawaiian Standard Time" },

                // Europe
                { "Europe/London", "GMT Standard Time" },
                { "Europe/Paris", "Central European Standard Time" },
                { "Europe/Berlin", "Central European Standard Time" },
                { "Europe/Amsterdam", "Central European Standard Time" },
                { "Europe/Brussels", "Central European Standard Time" },
                { "Europe/Vienna", "Central European Standard Time" },
                { "Europe/Prague", "Central European Standard Time" },
                { "Europe/Budapest", "Central European Standard Time" },
                { "Europe/Warsaw", "Central European Standard Time" },
                { "Europe/Moscow", "Russian Standard Time" },
                { "Europe/Dublin", "GMT Standard Time" },
                { "Europe/Lisbon", "GMT Standard Time" },
                { "Europe/Madrid", "Romance Standard Time" },
                { "Europe/Rome", "Romance Standard Time" },
                { "Europe/Stockholm", "Central European Standard Time" },
                { "Europe/Athens", "GTB Standard Time" },
                { "Europe/Istanbul", "Turkey Standard Time" },

                // Asia
                { "Asia/Tokyo", "Tokyo Standard Time" },
                { "Asia/Shanghai", "China Standard Time" },
                { "Asia/Hong_Kong", "China Standard Time" },
                { "Asia/Singapore", "Singapore Standard Time" },
                { "Asia/Bangkok", "Indochina Standard Time" },
                { "Asia/Dubai", "Arabian Standard Time" },
                { "Asia/Kolkata", "India Standard Time" },
                { "Asia/Seoul", "Korea Standard Time" },

                // Australia
                { "Australia/Sydney", "AUS Eastern Standard Time" },
                { "Australia/Melbourne", "AUS Eastern Standard Time" },
                { "Australia/Brisbane", "E. Australia Standard Time" },
                { "Australia/Perth", "W. Australia Standard Time" },
                { "Australia/Adelaide", "Cen. Australia Standard Time" },

                // Others
                { "UTC", "UTC" },
                { "Etc/UTC", "UTC" },
            };

            // Try direct mapping
            if (ianaToWindowsMap.TryGetValue(ianaTimezoneName, out var windowsName))
            {
                return windowsName;
            }

            // If not found, try to use the IANA name directly (works on Linux, might fail on Windows)
            // Verify it's a valid timezone
            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(ianaTimezoneName);
                return ianaTimezoneName;
            }
            catch
            {
                // Default to Eastern Standard Time if we can't find it
                _logger.LogWarning("Timezone {TimezoneName} not found. Defaulting to Eastern Standard Time.", ianaTimezoneName);
                return "Eastern Standard Time";
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
