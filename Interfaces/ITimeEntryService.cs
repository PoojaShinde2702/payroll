using TimeTrackingAPI.Models;

namespace TimeTrackingAPI.Interfaces
{
    public interface ITimeEntryService
    {
        Task<TimeEntry> ClockIn(int userId);
        Task<TimeEntry> ClockOut(int userId);
        Task<IEnumerable<TimeEntry>> GetUserTimeEntries(int userId);
    }
}
