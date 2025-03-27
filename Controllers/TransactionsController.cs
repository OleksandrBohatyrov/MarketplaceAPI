using Microsoft.AspNetCore.Mvc;

namespace FinalProject_Back_end.Controllers
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
