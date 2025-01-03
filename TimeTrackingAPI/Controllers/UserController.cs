using Data.Interface;
using Data.Repository;
using Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models;

namespace TimeTrackingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;

        public UserController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto model)
        {
            var user = await _userRepository.Register(model);
            return Ok(new { message = "Registration successful", userId = user.Id });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto model)
        {
            var user = await _userRepository.Login(model);
            if (user == null)
                return Unauthorized(new { message = "Invalid email or password" });

            return Ok(new { message = "Login successful", userId = user.Id });
        }
    }
}
