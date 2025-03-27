using Microsoft.AspNetCore.Mvc;

namespace FinalProject_Back_end.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetOrders() => Ok("All orders");

        [HttpGet("{id}")]
        public IActionResult GetOrder(int id) => Ok($"Order {id}");

        [HttpPost]
        public IActionResult CreateOrder() => Ok("Create order");
    }
}
