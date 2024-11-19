using TimeTrackingAPI.DTOs;
using TimeTrackingAPI.Models;

namespace TimeTrackingAPI.Interfaces
{
    public interface IUserService
    {
        Task<User> Register(RegisterDto model);
        Task<User> Login(LoginDto model);
    }
}
