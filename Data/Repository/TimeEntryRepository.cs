using Dapper;
using Data.Interface;
using Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Models;
using MySqlConnector;
using System.Data;

namespace TimeTrackingAPI.Repositories
{
    public class TimeEntryRepository : ITimeEntryRepository
    {
        private readonly IDbConnection _db;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;

        public TimeEntryRepository(IConfiguration configuration)
        {
            _configuration = configuration;
            _db = new MySqlConnection(configuration.GetConnectionString("DefaultConnection"));
          //  _logger = logger;
        }
        public async Task<TimeEntry> ClockIn(int userId)
        {
            try
            {
                var now = DateTime.Now;
                var today = now.Date;

                // Check if user already has an entry for today
                var checkSql = @"
                SELECT * FROM TimeEntries 
                WHERE UserId = @UserId 
                AND Date = @Date 
                AND Status = 'P'";

                var existingEntry = await _db.QueryFirstOrDefaultAsync<TimeEntry>(
                    checkSql,
                    new
                    {
                        UserId = userId,
                        Date = today.ToString("yyyy-MM-dd")
                    }
                );

                if (existingEntry != null)
                {
                    throw new InvalidOperationException("Already clocked in for today");
                }

                // Insert new time entry
                var insertSql = @"
                INSERT INTO TimeEntries 
                (UserId, Date, ClockIn, Status) 
                VALUES 
                (@UserId, @Date, @ClockIn, @Status);
                SELECT LAST_INSERT_ID();";

                var id = await _db.ExecuteScalarAsync<int>(
                    insertSql,
                    new
                    {
                        UserId = userId,
                        Date = today.ToString("yyyy-MM-dd"),
                        ClockIn = now.ToString("HH:mm:ss"),
                        Status = "P"
                    }
                );

                // Retrieve the inserted entry
                var selectSql = @"
                SELECT 
                    Id,
                    UserId,
                    Date,
                    STR_TO_DATE(CONCAT(Date, ' ', ClockIn), '%Y-%m-%d %H:%i:%s') as ClockIn,
                    STR_TO_DATE(CONCAT(Date, ' ', ClockOut), '%Y-%m-%d %H:%i:%s') as ClockOut,
                    Duration,
                    Status
                FROM TimeEntries 
                WHERE Id = @Id";

                var entry = await _db.QueryFirstOrDefaultAsync<TimeEntry>(
                    selectSql,
                    new { Id = id }
                );

                _logger.LogInformation($"User {userId} clocked in successfully at {now.ToString("hh:mm tt")}");
                return entry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during clock-in for user {userId}");
                throw;
            }
        }

        public async Task<TimeEntry> ClockOut(int userId)
        {
            try
            {
                var now = DateTime.Now;
                var today = now.Date;

                // Find active entry
                var selectSql = @"
                SELECT 
                    Id,
                    UserId,
                    Date,
                    STR_TO_DATE(CONCAT(Date, ' ', ClockIn), '%Y-%m-%d %H:%i:%s') as ClockIn,
                    STR_TO_DATE(CONCAT(Date, ' ', ClockOut), '%Y-%m-%d %H:%i:%s') as ClockOut,
                    Duration,
                    Status
                FROM TimeEntries 
                WHERE UserId = @UserId 
                AND Date = @Date 
                AND Status = 'P' 
                AND ClockOut IS NULL";

                var activeEntry = await _db.QueryFirstOrDefaultAsync<TimeEntry>(
                    selectSql,
                    new
                    {
                        UserId = userId,
                        Date = today.ToString("yyyy-MM-dd")
                    }
                );

                if (activeEntry == null)
                {
                    throw new InvalidOperationException("No active clock-in found for today");
                }

                // Calculate duration
                var duration = now - activeEntry.ClockIn.Value;
                var formattedDuration = $"{(int)duration.TotalHours} Hours {duration.Minutes} mins";

                var updateSql = @"
UPDATE TimeEntries 
SET ClockOut = @ClockOut,
    Duration = @Duration,
    Status = 'AC'  -- Change status to 'AC' (Auto Completed)
WHERE Id = @Id";

                await _db.ExecuteAsync(
                    updateSql,
                    new
                    {
                        Id = activeEntry.Id,
                        ClockOut = now.ToString("HH:mm:ss"),
                        Duration = formattedDuration
                    }
                );

                return await _db.QueryFirstOrDefaultAsync<TimeEntry>(selectSql,
                    new
                    {
                        UserId = userId,
                        Date = today.ToString("yyyy-MM-dd")
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during clock-out for user {userId}");
                throw;
            }
        }
        public async Task ProcessAutoClockOut()
        {
            var now = DateTime.Now;
            var autoClockOutTime = new TimeSpan(21, 0, 0); // 9 PM

            if (now.TimeOfDay >= autoClockOutTime)
            {
                var sql = @"UPDATE TimeEntries 
                                SET ClockOut = @ClockOut,
                                    Status = 'AC',
                                    Duration = @Duration
                                WHERE Date = @Date 
                                AND Status = 'P' 
                                AND ClockOut IS NULL";

                await _db.ExecuteAsync(sql, new
                {
                    Date = now.Date,
                    ClockOut = autoClockOutTime,
                    Duration = "Auto Completed"
                });
            }
        }
        public async Task ProcessAbsentees()
        {
            try
            {
                var sql = @"
                    INSERT INTO TimeEntries (UserId, Date, Status)
                    SELECT 
                        u.Id, 
                        CURDATE(), 
                        'A'
                    FROM Users u
                    WHERE NOT EXISTS (
                        SELECT 1 
                        FROM TimeEntries t 
                        WHERE t.UserId = u.Id 
                        AND t.Date = CURDATE()
                    )";

                await _db.ExecuteAsync(sql);

                _logger.LogInformation($"Processed absences for {DateTime.Now:yyyy-MM-dd}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing absentees");
                throw;
            }
        }
        public async Task<TimeEntryDto> GetUserStats(int userId, DateTime startDate, DateTime endDate)
        {
            var sql = @"
                SELECT 
                    COUNT(DISTINCT Date) as TotalDays,
                    SUM(CASE WHEN Status = 'P' THEN 1 ELSE 0 END) as PresentDays,
                    SUM(CASE WHEN Status = 'A' THEN 1 ELSE 0 END) as AbsentDays,
                    SUM(CASE WHEN Status = 'AC' THEN 1 ELSE 0 END) as AutoCompletedDays,
                    SEC_TO_TIME(SUM(TIME_TO_SEC(Duration))) as TotalWorkDuration,
                    MAX(CASE WHEN Status IN ('P', 'AC') THEN ClockIn END) as LastClockIn,
                    MAX(CASE WHEN Status IN ('P', 'AC') THEN ClockOut END) as LastClockOut
                FROM TimeEntries
                WHERE UserId = @UserId
                AND Date BETWEEN @StartDate AND @EndDate";

            var stats = await _db.QuerySingleAsync<TimeEntryDto>(sql, new
            {
                UserId = userId,
                StartDate = startDate,
                EndDate = endDate
            });

            // Calculate average work hours
            if (stats.PresentDays > 0)
            {
                stats.AverageWorkHours = stats.TotalWorkDuration.TotalHours / stats.PresentDays;
            }

            return stats;
        }

        public async Task<IEnumerable<TimeEntry>> GetDateRangeEntries(int userId, DateTime startDate, DateTime endDate)
        {
            var sql = @"
                SELECT *
                FROM TimeEntries
                WHERE UserId = @UserId
                AND Date BETWEEN @StartDate AND @EndDate
                ORDER BY Date DESC, ClockIn DESC";

            return await _db.QueryAsync<TimeEntry>(sql, new
            {
                UserId = userId,
                StartDate = startDate,
                EndDate = endDate
            });
        }

        public async Task<IEnumerable<TimeEntry>> GetUserTimeEntries(int userId)
        {
            return await _db.QueryAsync<TimeEntry>(
                @"SELECT 
                Id, UserId, Date,
                ClockIn,
                ClockOut,
                Duration,
                Status
            FROM TimeEntries 
            WHERE UserId = @UserId 
            ORDER BY Date DESC, ClockIn DESC",
                new { UserId = userId }
            );
        }
        public async Task ProcessAutoCheckouts()
        {
            var now = DateTime.Now;
            var autoCheckoutTime = new TimeSpan(21, 0, 0); // 9 PM

            if (now.TimeOfDay >= autoCheckoutTime)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@AutoCheckoutTime", now.Date.Add(autoCheckoutTime));
                parameters.Add("@FormattedCheckoutTime", "09:00 PM");
                parameters.Add("@Date", now.Date);

                const string sql = @"
                            UPDATE TimeEntries 
                            SET 
                                ClockOut = @AutoCheckoutTime,
                                FormattedClockOut = @FormattedCheckoutTime,
                                Status = 'AC',
                                Duration = TIMEDIFF(@AutoCheckoutTime, ClockIn)
                            WHERE Date = @Date
                            AND Status = 'P'
                            AND ClockOut IS NULL";

                await _db.ExecuteAsync(sql, parameters);
            }
        }

        public async Task MarkAbsentUsers(DateTime date)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Date", date);
            parameters.Add("@Status", "A");

            const string sql = @"
                        INSERT INTO TimeEntries (UserId, Date, Status)
                        SELECT Id, @Date, @Status
                        FROM Users u
                        WHERE NOT EXISTS (
                            SELECT 1 
                            FROM TimeEntries t 
                            WHERE t.UserId = u.Id 
                            AND t.Date = @Date
                        )";

            await _db.ExecuteAsync(sql, parameters);
        }
    }
}
//        private readonly IDbConnection _db;

//        public TimeEntryRepository(IDbConnection db)
//        {
//            _db = db;
//        }

//        public async Task<TimeEntry> ClockIn(int userId)
//        {
//            var now = DateTime.Now;

//            // Check for existing active entry
//            var existingEntry = await _db.QueryFirstOrDefaultAsync<TimeEntry>(@"
//                SELECT * FROM TimeEntries 
//                WHERE UserId = @UserId 
//                AND Date = @Date 
//                AND Status = 'P'",
//                new { UserId = userId, Date = now.Date });

//            if (existingEntry != null)
//            {
//                throw new InvalidOperationException("Already clocked in for today");
//            }

//            var parameters = new DynamicParameters();
//            parameters.Add("@UserId", userId);
//            parameters.Add("@Date", now.Date);
//            parameters.Add("@ClockIn", now);
//            parameters.Add("@FormattedClockIn", now.ToString("hh:mm tt"));
//            parameters.Add("@Status", "P");

//            const string sql = @"
//                INSERT INTO TimeEntries (UserId, Date, ClockIn, FormattedClockIn, Status) 
//                VALUES (@UserId, @Date, @ClockIn, @FormattedClockIn, @Status);
//                SELECT LAST_INSERT_ID();";

//            var id = await _db.ExecuteScalarAsync<int>(sql, parameters);

//            return await _db.QueryFirstOrDefaultAsync<TimeEntry>(
//                "SELECT * FROM TimeEntries WHERE Id = @Id",
//                new { Id = id });
//        }

//        public async Task<TimeEntry> ClockOut(int userId)
//        {
//            var now = DateTime.Now;

//            var parameters = new DynamicParameters();
//            parameters.Add("@UserId", userId);
//            parameters.Add("@ClockOut", now);
//            parameters.Add("@FormattedClockOut", now.ToString("hh:mm tt"));
//            parameters.Add("@Date", now.Date);

//            const string sql = @"
//                UPDATE TimeEntries 
//                SET 
//                    ClockOut = @ClockOut,
//                    FormattedClockOut = @FormattedClockOut,
//                    Duration = TIMEDIFF(@ClockOut, ClockIn),
//                    Status = 'P'
//                WHERE UserId = @UserId 
//                AND Date = @Date
//                AND Status = 'P'
//                AND ClockOut IS NULL;

//                SELECT * FROM TimeEntries
//                WHERE UserId = @UserId 
//                AND Date = @Date
//                AND Status = 'P'
//                ORDER BY ClockIn DESC 
//                LIMIT 1;";

//            return await _db.QueryFirstOrDefaultAsync<TimeEntry>(sql, parameters);
//        }

//        public async Task<TimeEntry> GetActiveTimeEntry(int userId)
//        {
//            const string sql = @"
//                SELECT * FROM TimeEntries 
//                WHERE UserId = @UserId 
//                AND Status = 'P'
//                AND Date = @Date
//                ORDER BY ClockIn DESC
//                LIMIT 1";

//            return await _db.QueryFirstOrDefaultAsync<TimeEntry>(sql,
//                new { UserId = userId, Date = DateTime.Now.Date });
//        }

//        public async Task<IEnumerable<TimeEntry>> GetUserTimeEntries(int userId)
//        {
//            const string sql = @"
//                SELECT * FROM TimeEntries 
//                WHERE UserId = @UserId 
//                ORDER BY Date DESC, ClockIn DESC";

//            return await _db.QueryAsync<TimeEntry>(sql, new { UserId = userId });
//        }

//        public async Task ProcessAutoCheckouts()
//        {
//            var now = DateTime.Now;
//            var autoCheckoutTime = new TimeSpan(21, 0, 0); // 9 PM

//            if (now.TimeOfDay >= autoCheckoutTime)
//            {
//                var parameters = new DynamicParameters();
//                parameters.Add("@AutoCheckoutTime", now.Date.Add(autoCheckoutTime));
//                parameters.Add("@FormattedCheckoutTime", "09:00 PM");
//                parameters.Add("@Date", now.Date);

//                const string sql = @"
//                    UPDATE TimeEntries 
//                    SET 
//                        ClockOut = @AutoCheckoutTime,
//                        FormattedClockOut = @FormattedCheckoutTime,
//                        Status = 'AC',
//                        Duration = TIMEDIFF(@AutoCheckoutTime, ClockIn)
//                    WHERE Date = @Date
//                    AND Status = 'P'
//                    AND ClockOut IS NULL";

//                await _db.ExecuteAsync(sql, parameters);
//            }
//        }

//        public async Task MarkAbsentUsers(DateTime date)
//        {
//            var parameters = new DynamicParameters();
//            parameters.Add("@Date", date);
//            parameters.Add("@Status", "A");

//            const string sql = @"
//                INSERT INTO TimeEntries (UserId, Date, Status)
//                SELECT Id, @Date, @Status
//                FROM Users u
//                WHERE NOT EXISTS (
//                    SELECT 1 
//                    FROM TimeEntries t 
//                    WHERE t.UserId = u.Id 
//                    AND t.Date = @Date
//                )";

//            await _db.ExecuteAsync(sql, parameters);
//        }
//    }
//}