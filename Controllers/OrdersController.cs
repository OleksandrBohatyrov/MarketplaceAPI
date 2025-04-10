using MarketplaceAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MarketplaceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public OrdersController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult GetOrders()
        {
            // admin can see all orders but user only it's own
            if (User.IsInRole("Admin"))
            {
                var orders = _db.Orders.ToList();
                return Ok(orders);
            }
            else
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var orders = _db.Orders.Where(o => o.BuyerId == currentUserId).ToList();
                return Ok(orders);
            }
        }

    }
}
