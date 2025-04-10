using MarketplaceAPI.Data;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using MarketplaceAPI.Models;


namespace MarketplaceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public TransactionsController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult GetTransactions()
        {
            // Admin can see all transactions, but user only his
            if (User.IsInRole("Admin"))
            {
                var transactions = _db.Transactions.ToList();
                return Ok(transactions);
            }
            else
            {
                var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
                var transactions = from t in _db.Transactions
                                   join o in _db.Orders on t.OrderId equals o.Id
                                   where o.BuyerId == userId
                                   select t;
                return Ok(transactions.ToList());
            }


        }


        [HttpPost("pay")]
        public IActionResult Pay([FromBody] Transaction transaction)
        {
            if (transaction == null)
                return BadRequest(new { message = "Incorrect transaction data" });

            // Make as Completed
            // TODO: In future make real payment system
            transaction.Status = "Completed";
            _db.Transactions.Add(transaction);
            _db.SaveChanges();

            return Ok(new { message = "Payment successfully processed", transaction });
        }
    }
}
