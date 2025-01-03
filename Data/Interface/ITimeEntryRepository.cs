using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Models;

namespace Data.Interface
{
    public interface ITimeEntryRepository
    {
        Task<TimeEntry> ClockIn(int userId);
        Task<TimeEntry> ClockOut(int userId);
        Task<IEnumerable<TimeEntry>> GetUserTimeEntries(int userId);
        Task ProcessAutoCheckouts();
        Task MarkAbsentUsers(DateTime date);
    }
}