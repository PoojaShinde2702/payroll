// Controllers/TimeEntryController.cs
using Microsoft.AspNetCore.Mvc;
using TimeTrackingAPI.Interfaces;

namespace TimeTrackingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TimeEntryController : ControllerBase
    {
        private readonly ITimeEntryService _timeEntryService;

        public TimeEntryController(ITimeEntryService timeEntryService)
        {
            _timeEntryService = timeEntryService;
        }

        [HttpPost("clockin")]
        public async Task<IActionResult> ClockIn([FromQuery] int userId)
        {
            var entry = await _timeEntryService.ClockIn(userId);
            return Ok(new { message = "Clocked in successfully", entry });
        }

        [HttpPost("clockout")]
        public async Task<IActionResult> ClockOut([FromQuery] int userId)
        {
            var entry = await _timeEntryService.ClockOut(userId);
            return Ok(new { message = "Clocked out successfully", entry });
        }

        [HttpGet("entries")]
        public async Task<IActionResult> GetEntries([FromQuery] int userId)
        {
            var entries = await _timeEntryService.GetUserTimeEntries(userId);
            return Ok(entries);
        }
    }
}