namespace TimeTrackingAPI.DTOs
{
    public class TimeEntryDto
    {
        public DateTime Date { get; set; }
        public string Status { get; set; }
        public DateTime? ClockIn { get; set; }
        public DateTime? ClockOut { get; set; }
        public TimeSpan? Duration { get; set; }
    }
}
