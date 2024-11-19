using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TimeTrackingAPI.DTOs;
using TimeTrackingAPI.Interfaces;
using TimeTrackingAPI.Services;

namespace TimeTrackingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto model)
        {
            var user = await _userService.Register(model);
            return Ok(new { message = "Registration successful", userId = user.Id });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto model)
        {
            var user = await _userService.Login(model);
            if (user == null)
                return Unauthorized(new { message = "Invalid email or password" });

            return Ok(new { message = "Login successful", userId = user.Id });
        }
    }
}