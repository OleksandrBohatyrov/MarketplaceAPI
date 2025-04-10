using MarketplaceAPI.Models;
using MarketplaceAPI.Data;
using Microsoft.AspNetCore.Mvc;
using BCrypt.Net;
using Org.BouncyCastle.Crypto.Generators;

namespace MarketplaceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public AuthController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterModel model)
        {
            if (_db.Users.Any(u => u.Email == model.Email))
                return BadRequest("Email already exists");

            var user = new ApplicationUser
            {
                UserName = model.Username,
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password)
            };

            _db.Users.Add(user);
            _db.SaveChanges();

            return Ok("User registered successfully");
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginModel model)
        {
            var user = _db.Users.FirstOrDefault(u => u.Email == model.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                return Unauthorized("Invalid email or password");

            return Ok("Login successful");
        }
    }
}
