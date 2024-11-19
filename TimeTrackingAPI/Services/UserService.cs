using TimeTrackingAPI.Interfaces;
using TimeTrackingAPI.Models;
using TimeTrackingAPI.DTOs;
using Dapper;
using MySql.Data.MySqlClient;
using System.Data;

namespace TimeTrackingAPI.Services
{
    public class UserService : IUserService
    {
        private readonly IDbConnection _db;

        public UserService(IConfiguration configuration)
        {
            _db = new MySqlConnection(configuration.GetConnectionString("DefaultConnection"));
        }

        public async Task<User> Register(RegisterDto model)
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

            var sql = @"INSERT INTO Users (UserName, Email, PasswordHash) 
                       VALUES (@UserName, @Email, @PasswordHash);
                       SELECT LAST_INSERT_ID();";

            var userId = await _db.ExecuteScalarAsync<int>(sql, new
            {
                model.UserName,
                model.Email,
                PasswordHash = passwordHash
            });

            return await _db.QueryFirstAsync<User>(
                "SELECT * FROM Users WHERE Id = @Id",
                new { Id = userId }
            );
        }

        public async Task<User> Login(LoginDto model)
        {
            var user = await _db.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM Users WHERE Email = @Email",
                new { model.Email }
            );

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                return null;

            return user;
        }
    }
}