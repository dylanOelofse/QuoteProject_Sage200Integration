using Pastel.Evolution;
using QuotesProject.Interfaces;
using QuotesProject.Models;
using QuotesProject.Data;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;

namespace QuotesProject.Services
{
    public class UserService : IUserService
    {
        public User ValidateUser(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username is required");


            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password is required");

            DataTable userTable = DatabaseEngine.GetUserByUsername(username);

            if (userTable.Rows.Count == 0)
                throw new UnauthorizedAccessException("User not found");

            User user = new User
            {
                UserId = Convert.ToInt32(userTable.Rows[0]["UserId"]),
                Username = userTable.Rows[0]["Username"].ToString(),
                Password = userTable.Rows[0]["Password"].ToString(),
                IsAdmin = Convert.ToInt32(userTable.Rows[0]["IsAdmin"])
            };

            // Re - hash the supplied password and compare against the stored hash
            var hasher = new PasswordHasher<User>();
            var result = hasher.VerifyHashedPassword(user, user.Password, password);

            if (result == PasswordVerificationResult.Failed)
                throw new UnauthorizedAccessException("Invalid username or password");

            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                var upgradedPassword = hasher.HashPassword(user, password);
                DatabaseEngine.UpdatePasswordHash(user.UserId, upgradedPassword);
            }

            user.Password = null; // don't hand the hash back to callers

            return user;
        }

        public void CreateUser(User user)
        {
            if (string.IsNullOrWhiteSpace(user.Username))
                throw new ArgumentException("Username is required");

            if (string.IsNullOrWhiteSpace(user.Password))
                throw new ArgumentException("Password is required");

            if (DatabaseEngine.GetUserByUsername(user.Username).Rows.Count > 0)
                throw new ArgumentException("Username is already taken.");    

            // Hash the password before storing it
            var hasher = new PasswordHasher<User>();
            var hashedPassword = hasher.HashPassword(user, user.Password);

            user.Password = hashedPassword;

            DatabaseEngine.CreateUser(user);
        }

    }
}
