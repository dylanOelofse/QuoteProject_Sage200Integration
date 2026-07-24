using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuotesProject.Interfaces;
using QuotesProject.Models;

namespace QuotesProject.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public IActionResult CreateUser(User user)
        {
            if (!ModelState.IsValid) 
                return BadRequest(ModelState);

            try
            {
                _userService.CreateUser(user);
                return Ok("User created succesfully: " + user);

            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error creating user: " + ex.Message);
            }
        }
    }
}
