using QuotesProject.Models;

namespace QuotesProject.Interfaces
{
    public interface IUserService
    {
        User ValidateUser(string username, string password);

        void CreateUser(User user);
    }
}
