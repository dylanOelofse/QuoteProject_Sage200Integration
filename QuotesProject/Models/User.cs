using System.ComponentModel.DataAnnotations;

namespace QuotesProject.Models
{
    public class User
    {
        public int UserId {  get; set; }
        public string Username { get; set; }
        public string? Password { get; set; }
        public int IsAdmin { get; set; }
    }
}
