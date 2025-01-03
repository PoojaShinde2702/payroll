using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class TimeEntry
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime Date { get; set; }
        public DateTime? ClockIn { get; set; }
        public DateTime? ClockOut { get; set; }
        public string Duration { get; set; }
        public string Status { get; set; }

        // Format for display
        public string FormattedClockIn => ClockIn?.ToString("hh:mm tt");
        public string FormattedClockOut => ClockOut?.ToString("hh:mm tt");

        public void CalculateDuration()
        {
            if (ClockIn.HasValue && ClockOut.HasValue)
            {
                var duration = ClockOut.Value - ClockIn.Value;
                int hours = (int)duration.TotalHours;
                int minutes = duration.Minutes;
                Duration = $"{hours} Hours {minutes} mins";
            }
        }
    }
}