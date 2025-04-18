using MarketplaceAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MarketplaceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UsersController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            // получаем id из токена
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();

            // достаем самого пользователя
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound("User not found");

            // получаем список ролей
            var roles = await _userManager.GetRolesAsync(user);

            // формируем DTO
            var result = new
            {
                id = user.Id,
                fullName = user.FullName,
                userName = user.UserName,
                email = user.Email,
                roles = roles
            };

            return Ok(result);
        }
    
}

}