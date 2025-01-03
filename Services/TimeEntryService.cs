using Dapper;
using Data.Interface;
using Microsoft.Extensions.Configuration;
using Models;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Services
{
    public class TimeEntryService : ITimeEntryService
    {
        private readonly IDbConnection _db;
        private readonly IConfiguration _configuration;
        private readonly TimeSpan _autoCheckoutDelay = TimeSpan.FromHours(12); // 12 hours from login

        public TimeEntryService(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _db = new MySqlConnection(configuration.GetConnectionString("DefaultConnection"));
        }

        public async Task<TimeEntry> ClockIn(int userId)
        {
            var now = DateTime.Now;
            var today = now.Date;

            // Check if user already has an active session
            var activeEntry = await _db.QueryFirstOrDefaultAsync<TimeEntry>(
                @"SELECT * FROM TimeEntries 
                  WHERE UserId = @UserId 
                  AND Date = @Today
                  AND Status = 'P'",
                new { UserId = userId, Today = today });

            if (activeEntry != null)
            {
                throw new InvalidOperationException("User has already clocked in today.");
            }

            var formattedClockIn = now.ToString("hh:mm tt"); // Format time as AM/PM

            var sql = @"INSERT INTO TimeEntries 
                       (UserId, Date, ClockIn, FormattedClockIn, Status) 
                       VALUES 
                       (@UserId, @Date, @ClockIn, @FormattedClockIn, @Status);
                       SELECT LAST_INSERT_ID();";

            var id = await _db.ExecuteScalarAsync<int>(sql, new
            {
                UserId = userId,
                Date = today,
                ClockIn = now,
                FormattedClockIn = formattedClockIn,
                Status = "P" // Present status
            });

            return await _db.QueryFirstAsync<TimeEntry>(
                @"SELECT Id, UserId, Date, 
                         ClockIn, ClockOut, Duration, Status,
                         FormattedClockIn, FormattedClockOut
                  FROM TimeEntries 
                  WHERE Id = @Id",
                new { Id = id });
        }

        public async Task<TimeEntry> ClockOut(int userId)
        {
            var now = DateTime.Now;
            var today = now.Date;

            // Find active entry
            var activeEntry = await _db.QueryFirstOrDefaultAsync<TimeEntry>(
                @"SELECT * FROM TimeEntries 
                  WHERE UserId = @UserId 
                  AND Date = @Date 
                  AND Status = 'P' 
                  AND ClockOut IS NULL",
                new { UserId = userId, Date = today });

            if (activeEntry == null)
            {
                throw new InvalidOperationException("No active clock-in found for today.");
            }

            // Calculate duration
            var duration = now - activeEntry.ClockIn;
            var formattedDuration = $"{(int)duration.Value.TotalHours} Hours {duration.Value.Minutes} mins";
            var formattedClockOut = now.ToString("hh:mm tt"); // Format time as AM/PM

            // Update the entry
            var updateSql = @"UPDATE TimeEntries 
                             SET ClockOut = @ClockOut,
                                 FormattedClockOut = @FormattedClockOut,
                                 Duration = @Duration,
                                 Status = 'P'
                             WHERE Id = @Id";

            await _db.ExecuteAsync(updateSql, new
            {
                Id = activeEntry.Id,
                ClockOut = now,
                FormattedClockOut = formattedClockOut,
                Duration = formattedDuration
            });

            // Return updated entry
            return await _db.QueryFirstAsync<TimeEntry>(
                @"SELECT Id, UserId, Date, 
                         ClockIn, ClockOut, Duration, Status,
                         FormattedClockIn, FormattedClockOut
                  FROM TimeEntries 
                  WHERE Id = @Id",
                new { Id = activeEntry.Id });
        }

        public async Task<IEnumerable<TimeEntry>> GetUserTimeEntries(int userId)
        {
            var sql = @"SELECT Id, UserId, Date, 
                              ClockIn, ClockOut, Duration, Status,
                              FormattedClockIn, FormattedClockOut
                       FROM TimeEntries 
                       WHERE UserId = @UserId 
                       ORDER BY Date DESC, ClockIn DESC";

            return await _db.QueryAsync<TimeEntry>(sql, new { UserId = userId });
        }

        public async Task ProcessAutoClockOut()
        {
            var now = DateTime.Now;

            // Get all active sessions that have been open for more than 12 hours
            var activeSessions = await _db.QueryAsync<TimeEntry>(
                @"SELECT * FROM TimeEntries 
                  WHERE Date = @Today 
                  AND Status = 'P' 
                  AND ClockOut IS NULL
                  AND TIMESTAMPDIFF(HOUR, ClockIn, NOW()) >= 12",
                new { Today = now.Date });

            foreach (var session in activeSessions)
            {
                var autoCheckoutTime = session.ClockIn.Value.Add(_autoCheckoutDelay);
                var duration = autoCheckoutTime - session.ClockIn;
                var formattedDuration = $"{(int)duration.Value.TotalHours} Hours {duration.Value.Minutes} mins";
                var formattedClockOut = autoCheckoutTime.ToString("hh:mm tt");

                await _db.ExecuteAsync(
                    @"UPDATE TimeEntries 
                      SET ClockOut = @AutoCheckoutTime,
                          FormattedClockOut = @FormattedClockOut,
                          Duration = @Duration,
                          Status = 'AC'
                      WHERE Id = @Id",
                    new
                    {
                        Id = session.Id,
                        AutoCheckoutTime = autoCheckoutTime,
                        FormattedClockOut = formattedClockOut,
                        Duration = formattedDuration
                    });
            }
        }

        public async Task ProcessAbsentees()
        {
            var now = DateTime.Now;
            if (now.TimeOfDay >= new TimeSpan(10, 0, 0)) // After 10 AM
            {
                var sql = @"INSERT INTO TimeEntries (UserId, Date, Status)
                           SELECT Id, @Today, 'A'
                           FROM Users u
                           WHERE NOT EXISTS (
                               SELECT 1 
                               FROM TimeEntries t 
                               WHERE t.UserId = u.Id 
                               AND t.Date = @Today
                           )";

                await _db.ExecuteAsync(sql, new { Today = now.Date });
            }
        }
    }
}