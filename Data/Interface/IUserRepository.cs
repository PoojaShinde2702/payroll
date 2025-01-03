using Dtos;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Interface
{
    public interface IUserRepository
    {
        Task<User> Register(RegisterDto model);
        Task<User> Login(LoginDto model);
    }
}
