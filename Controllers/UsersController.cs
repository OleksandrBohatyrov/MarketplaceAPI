using Microsoft.AspNetCore.Mvc;

namespace MarketplaceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetAllUsers() => Ok("All users");

        [HttpGet("{id}")]
        public IActionResult GetUserById(int id) => Ok($"User {id}");

        [HttpPut("{id}")]
        public IActionResult UpdateUser(int id) => Ok($"Update user {id}");

        [HttpDelete("{id}")]
        public IActionResult DeleteUser(int id) => Ok($"Delete user {id}");
    }
}
