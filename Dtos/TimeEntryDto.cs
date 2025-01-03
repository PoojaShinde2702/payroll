using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtos
{
    public class TimeEntryDto
    {
        public DateTime Date { get; set; }
        public string Status { get; set; }
        public DateTime? ClockIn { get; set; }
        public DateTime? ClockOut { get; set; }
        public TimeSpan? Duration { get; set; }
        public int TotalDays { get; set; }
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int AutoCompletedDays { get; set; }
        public TimeSpan TotalWorkDuration { get; set; }
        public double AverageWorkHours { get; set; }
        public DateTime? LastClockIn { get; set; }
        public DateTime? LastClockOut { get; set; }
    }
}
