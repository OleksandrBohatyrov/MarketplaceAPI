using MarketplaceAPI.Data;
using MarketplaceAPI.Models;
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
        [HttpGet("{id}")]
        public IActionResult GetOrder(int id)
        {
            var order = _db.Orders.FirstOrDefault(o => o.Id == id);
            if (order == null)
                return NotFound(new { message = "Order not found" });

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (order.BuyerId != currentUserId && !User.IsInRole("Admin"))
                return Forbid();

            return Ok(order);
        }

        [HttpPost]
        public IActionResult CreateOrder([FromBody] Order order)
        {
            if (order == null)
                return BadRequest(new { message = "Incorrect order data" });

            order.BuyerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            order.OrderDate = DateTime.UtcNow;
            _db.Orders.Add(order);
            _db.SaveChanges();

            return Ok(order);
        }

    }
}
