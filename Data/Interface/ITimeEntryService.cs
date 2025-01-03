using Dtos;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Interface
{
    public interface ITimeEntryService
    {
        Task<TimeEntry> ClockIn(int userId);
        Task<TimeEntry> ClockOut(int userId);
        Task<IEnumerable<TimeEntry>> GetUserTimeEntries(int userId);

        Task ProcessAutoClockOut();
        Task ProcessAbsentees();
    }
}