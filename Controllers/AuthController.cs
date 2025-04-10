using MarketplaceAPI.Models;
using MarketplaceAPI.Data;
using Microsoft.AspNetCore.Mvc;
using BCrypt.Net;
using Org.BouncyCastle.Crypto.Generators;
using Microsoft.AspNetCore.Identity;

namespace MarketplaceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = new ApplicationUser
            {
                UserName = model.Username,
                Email = model.Email,
                FullName = model.Username 
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                // set role user as a default
                await _userManager.AddToRoleAsync(user, "User");
                return Ok("User succesfully registered");
            }

            return BadRequest(result.Errors);
        }


    }
}
