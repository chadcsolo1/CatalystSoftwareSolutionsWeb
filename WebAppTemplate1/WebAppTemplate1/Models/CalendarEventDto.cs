namespace WebAppTemplate1.Models
{
    public class CalendarEventDto
    {
        public string Id { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public string? Description { get; set; }
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
        public string? OrganizerEmail { get; set; }
        public string? CreatorEmail { get; set; }
        public string? HtmlLink { get; set; }
    }
}
