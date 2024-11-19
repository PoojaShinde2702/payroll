using TimeTrackingAPI.Interfaces;
using TimeTrackingAPI.Models;
using Dapper;
using MySql.Data.MySqlClient;
using System.Data;

namespace TimeTrackingAPI.Services
{
    public class TimeEntryService : ITimeEntryService
    {
        private readonly IDbConnection _db;

        public TimeEntryService(IConfiguration configuration)
        {
            _db = new MySqlConnection(configuration.GetConnectionString("DefaultConnection"));
        }

        public async Task<TimeEntry> ClockIn(int userId)
        {
            var now = DateTime.Now;

            var sql = @"INSERT INTO TimeEntries (UserId, Date, ClockIn, Status) 
                       VALUES (@UserId, @Date, @ClockIn, 'Active');
                       SELECT LAST_INSERT_ID();";

            var id = await _db.ExecuteScalarAsync<int>(sql, new
            {
                UserId = userId,
                Date = now.Date,
                ClockIn = now
            });

            return await _db.QueryFirstAsync<TimeEntry>(
                "SELECT * FROM TimeEntries WHERE Id = @Id",
                new { Id = id }
            );
        }

        public async Task<TimeEntry> ClockOut(int userId)
        {
            var now = DateTime.Now;

            var sql = @"UPDATE TimeEntries 
                       SET ClockOut = @ClockOut, Status = 'Completed' 
                       WHERE UserId = @UserId 
                       AND Date = @Date 
                       AND Status = 'Active';";

            await _db.ExecuteAsync(sql, new
            {
                UserId = userId,
                Date = now.Date,
                ClockOut = now
            });

            return await _db.QueryFirstAsync<TimeEntry>(
                @"SELECT * FROM TimeEntries 
                  WHERE UserId = @UserId 
                  AND Date = @Date",
                new { UserId = userId, Date = now.Date }
            );
        }

        public async Task<IEnumerable<TimeEntry>> GetUserTimeEntries(int userId)
        {
            return await _db.QueryAsync<TimeEntry>(
                @"SELECT * FROM TimeEntries 
                  WHERE UserId = @UserId 
                  ORDER BY Date DESC",
                new { UserId = userId }
            );
        }
    }
}