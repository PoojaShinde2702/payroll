using BCrypt.Net;
using Dapper;
using Data.Interface;
using Dtos;
using Microsoft.Extensions.Configuration;
using Models;
using MySqlConnector;
using System;
using System.Data;
using System.Threading.Tasks;

namespace Data.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly IDbConnection _db;

        public UserRepository(IConfiguration configuration)
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
//        private readonly IDbConnection _db;

//        public UserRepository(IDbConnection db)
//        {
//            _db = db ?? throw new ArgumentNullException(nameof(db));
//        }

//        public async Task<User> RegisterAsync(User model)
//        {
//            try
//            {
//                // Hash the password before storing
//                var passwordHash = BCrypt.Net.BCrypt.HashPassword(model.PasswordHash);

//                const string insertSql = @"
//                    INSERT INTO Users (UserName, Email, PasswordHash) 
//                    VALUES (@UserName, @Email, @PasswordHash);
//                    SELECT LAST_INSERT_ID();";

//                // Insert the user and retrieve the generated ID
//                var userId = await _db.ExecuteScalarAsync<int>(insertSql, new
//                {
//                    model.UserName,
//                    model.Email,
//                    PasswordHash = passwordHash
//                });

//                // Retrieve the newly created user
//                return await _db.QueryFirstOrDefaultAsync<User>(
//                    "SELECT * FROM Users WHERE Id = @Id",
//                    new { Id = userId }
//                );
//            }
//            catch (Exception ex)
//            {
//                // Log the error (consider injecting a logging service)
//                throw new Exception("An error occurred while registering the user.", ex);
//            }
//        }

//        public async Task<User> LoginAsync(string email, string password)
//        {
//            try
//            {
//                const string selectSql = "SELECT * FROM Users WHERE Email = @Email";

//                // Retrieve the user by email
//                var user = await _db.QueryFirstOrDefaultAsync<User>(selectSql, new { Email = email });

//                // Verify the password
//                if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
//                {
//                    return null; // Return null if authentication fails
//                }

//                return user;
//            }
//            catch (Exception ex)
//            {
//                // Log the error
//                throw new Exception("An error occurred while logging in the user.", ex);
//            }
//        }

//        public async Task<bool> DeleteUserAsync(int id)
//        {
//            try
//            {
//                const string updateSql = "UPDATE Users SET IsActive = 0 WHERE Id = @Id";

//                // Mark the user as inactive
//                var rowsAffected = await _db.ExecuteAsync(updateSql, new { Id = id });

//                return rowsAffected > 0;
//            }
//            catch (Exception ex)
//            {
//                // Log the error
//                throw new Exception("An error occurred while deleting the user.", ex);
//            }
//        }
//    }
//}
