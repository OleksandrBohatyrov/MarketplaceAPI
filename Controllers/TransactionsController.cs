using Microsoft.AspNetCore.Mvc;

namespace MarketplaceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetTransactions() => Ok("All transactions");

        [HttpPost("pay")]
        public IActionResult Pay() => Ok("Simulated payment");
    }
}
