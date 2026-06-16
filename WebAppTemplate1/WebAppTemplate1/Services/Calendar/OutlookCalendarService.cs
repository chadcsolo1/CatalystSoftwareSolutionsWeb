using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Extensions.Options;
using WebAppTemplate1.Services.Email;

namespace WebAppTemplate1.Services.Calendar
{
    /// <summary>
    /// Outlook calendar service implementation using Microsoft Graph API.
    /// </summary>
    public class OutlookCalendarService : ICalendarService
    {
        private readonly GraphServiceClient _graphClient;
        private readonly ILogger<OutlookCalendarService> _logger;
        private readonly CalendarOptions _calendarOptions;
        private readonly string _calendarOwnerEmail;

        // Business hours configuration
        private readonly Dictionary<DayOfWeek, (TimeSpan Start, TimeSpan End)> _businessHours = new()
        {
            { DayOfWeek.Monday, (new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0)) },
            { DayOfWeek.Tuesday, (new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0)) },
            { DayOfWeek.Friday, (new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0)) }
        };

        private const int ConsultationDurationMinutes = 30;
        private const int BufferMinutes = 10;

        public OutlookCalendarService(
            IOptions<MicrosoftGraphOptions> graphOptions,
            IOptions<CalendarOptions> calendarOptions,
            ILogger<OutlookCalendarService> logger)
        {
            _calendarOptions = calendarOptions.Value;
            _logger = logger;
            _calendarOwnerEmail = _calendarOptions.CalendarOwnerEmail;

            // Initialize Graph client with client credentials
            var clientSecretCredential = new ClientSecretCredential(
                graphOptions.Value.TenantId,
                graphOptions.Value.ClientId,
                graphOptions.Value.ClientSecret);

            _graphClient = new GraphServiceClient(clientSecretCredential);
        }

        public async Task<List<string>> GetAvailableTimeSlotsAsync(DateTime date)
        {
            try
            {
                // Check if date is a business day
                if (!_businessHours.ContainsKey(date.DayOfWeek))
                {
                    _logger.LogInformation("Date {Date} is not a business day", date.ToShortDateString());
                    return new List<string>();
                }

                var (startTime, endTime) = _businessHours[date.DayOfWeek];

                // Convert to EST
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                var startDateTime = TimeZoneInfo.ConvertTimeToUtc(
                    new DateTime(date.Year, date.Month, date.Day, startTime.Hours, startTime.Minutes, 0),
                    timeZone);
                var endDateTime = TimeZoneInfo.ConvertTimeToUtc(
                    new DateTime(date.Year, date.Month, date.Day, endTime.Hours, endTime.Minutes, 0),
                    timeZone);

                // Get calendar view for the day
                var events = await _graphClient.Users[_calendarOwnerEmail]
                    .CalendarView
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.StartDateTime = startDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
                        requestConfiguration.QueryParameters.EndDateTime = endDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    });

                // Generate all possible time slots
                var allSlots = GenerateTimeSlots(startTime, endTime);

                // Filter out busy slots
                var availableSlots = new List<string>();
                foreach (var slot in allSlots)
                {
                    var slotTime = TimeSpan.Parse(slot);
                    var slotStart = TimeZoneInfo.ConvertTimeToUtc(
                        new DateTime(date.Year, date.Month, date.Day, slotTime.Hours, slotTime.Minutes, 0),
                        timeZone);
                    var slotEnd = slotStart.AddMinutes(ConsultationDurationMinutes);

                    bool isAvailable = true;

                    if (events?.Value != null)
                    {
                        foreach (var evt in events.Value)
                        {
                            if (evt.Start?.DateTime == null || evt.End?.DateTime == null)
                                continue;

                            var eventStart = DateTime.Parse(evt.Start.DateTime);
                            var eventEnd = DateTime.Parse(evt.End.DateTime);

                            // Check if slot overlaps with existing event (including buffer)
                            if (!(slotEnd.AddMinutes(BufferMinutes) <= eventStart || slotStart >= eventEnd.AddMinutes(BufferMinutes)))
                            {
                                isAvailable = false;
                                break;
                            }
                        }
                    }

                    if (isAvailable)
                    {
                        availableSlots.Add(slot);
                    }
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
                // Parse the time and create start/end times in EST
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                var timeParts = booking.ConsultationTime.Split(':');
                var hour = int.Parse(timeParts[0]);
                var minute = int.Parse(timeParts[1].Split(' ')[0]);

                // Handle AM/PM
                if (booking.ConsultationTime.Contains("PM") && hour != 12)
                {
                    hour += 12;
                }
                else if (booking.ConsultationTime.Contains("AM") && hour == 12)
                {
                    hour = 0;
                }

                var startTimeLocal = new DateTime(
                    booking.ConsultationDate.Year,
                    booking.ConsultationDate.Month,
                    booking.ConsultationDate.Day,
                    hour, minute, 0);

                var endTimeLocal = startTimeLocal.AddMinutes(ConsultationDurationMinutes);

                // Create event description with client information
                var description = $@"
Consultation with {booking.FullName}

Contact Information:
- Email: {booking.Email}
- Phone: {booking.Phone}
{(!string.IsNullOrWhiteSpace(booking.Company) ? $"- Company: {booking.Company}" : "")}

{(!string.IsNullOrWhiteSpace(booking.Message) ? $"Message:\n{booking.Message}" : "")}
".Trim();

                var newEvent = new Event
                {
                    Subject = $"Consultation: {booking.FullName}",
                    Body = new ItemBody
                    {
                        ContentType = BodyType.Text,
                        Content = description
                    },
                    Start = new DateTimeTimeZone
                    {
                        DateTime = startTimeLocal.ToString("yyyy-MM-ddTHH:mm:ss"),
                        TimeZone = "Eastern Standard Time"
                    },
                    End = new DateTimeTimeZone
                    {
                        DateTime = endTimeLocal.ToString("yyyy-MM-ddTHH:mm:ss"),
                        TimeZone = "Eastern Standard Time"
                    },
                    Location = new Location
                    {
                        DisplayName = "Virtual Consultation"
                    },
                    IsReminderOn = true,
                    ReminderMinutesBeforeStart = 15,
                    ShowAs = FreeBusyStatus.Busy
                };

                var createdEvent = await _graphClient.Users[_calendarOwnerEmail]
                    .Calendar
                    .Events
                    .PostAsync(newEvent);

                _logger.LogInformation("Created calendar event {EventId} for consultation with {ClientName}",
                    createdEvent?.Id, booking.FullName);

                return createdEvent?.Id ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating calendar event for consultation with {ClientName}", booking.FullName);
                throw;
            }
        }

        private List<string> GenerateTimeSlots(TimeSpan start, TimeSpan end)
        {
            var slots = new List<string>();
            var current = start;
            var totalMinutes = ConsultationDurationMinutes + BufferMinutes;

            while (current.Add(TimeSpan.FromMinutes(ConsultationDurationMinutes)) <= end)
            {
                var hour = current.Hours;
                var ampm = hour >= 12 ? "PM" : "AM";
                if (hour > 12) hour -= 12;
                if (hour == 0) hour = 12;

                slots.Add($"{hour:D2}:{current.Minutes:D2} {ampm}");
                current = current.Add(TimeSpan.FromMinutes(totalMinutes));
            }

            return slots;
        }
    }
}
