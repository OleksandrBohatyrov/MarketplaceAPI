using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MarketplaceAPI.Data;
using MarketplaceAPI.Models;

namespace MarketplaceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TradesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public TradesController(ApplicationDbContext db) => _db = db;

        // POST /api/trades
        // { requestedProductId: int, offeredProductId: int }
        [HttpPost]
        public IActionResult Create([FromBody] TradeDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // проверяем, что предложенный товар действительно ваш
            var mine = _db.Products.Any(p => p.Id == dto.OfferedProductId && p.SellerId == userId);
            if (!mine) return BadRequest("You can only offer your own product.");

            // проверяем, что запрашиваемый товар существует и не ваш
            var requested = _db.Products.Find(dto.RequestedProductId);
            if (requested == null) return NotFound("Requested product not found.");
            if (requested.SellerId == userId) return BadRequest("Cannot propose exchange on your own product.");

            var trade = new Trade
            {
                RequesterId = userId,
                OfferedProductId = dto.OfferedProductId,
                RequestedProductId = dto.RequestedProductId
            };

            _db.Trades.Add(trade);
            _db.SaveChanges();

            return Ok(trade);
        }

        // GET /api/trades/incoming
        // Список заявок на обмен, адресованных вам
        [HttpGet("incoming")]
        public IActionResult Incoming()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var list = _db.Trades
                .Include(t => t.Requester)
                .Include(t => t.OfferedProduct)
                .Include(t => t.RequestedProduct).ThenInclude(p => p.Category)
                .Where(t => t.RequestedProduct.SellerId == userId && t.Status == TradeStatus.Pending)
                .Select(t => new {
                    t.Id,
                    Requester = new { t.Requester.Id, t.Requester.UserName },
                    t.OfferedProductId,
                    OfferedName = t.OfferedProduct.Name,
                    t.RequestedProductId,
                    RequestedName = t.RequestedProduct.Name,
                    t.Status,
                    t.CreatedAt
                })
                .ToList();

            return Ok(list);
        }

        // PUT /api/trades/{id}/accept
        [HttpPut("{id}/accept")]
        public IActionResult Accept(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var trade = _db.Trades
                .Include(t => t.OfferedProduct)
                .Include(t => t.RequestedProduct)
                .FirstOrDefault(t => t.Id == id);

            if (trade == null) return NotFound();
            if (trade.RequestedProduct.SellerId != userId) return Forbid();
            if (trade.Status != TradeStatus.Pending) return BadRequest("Already processed.");

            // меняем статусы обоих товаров
            trade.OfferedProduct.Status = ProductStatus.Sold;
            trade.RequestedProduct.Status = ProductStatus.Sold;
            trade.Status = TradeStatus.Accepted;

            _db.SaveChanges();
            return Ok();
        }

        // PUT /api/trades/{id}/reject
        [HttpPut("{id}/reject")]
        public IActionResult Reject(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var trade = _db.Trades.Find(id);

            if (trade == null) return NotFound();
            if (trade.RequestedProduct.SellerId != userId) return Forbid();
            if (trade.Status != TradeStatus.Pending) return BadRequest("Already processed.");

            trade.Status = TradeStatus.Rejected;
            _db.SaveChanges();
            return Ok();
        }

        public class TradeDto
        {
            public int RequestedProductId { get; set; }
            public int OfferedProductId { get; set; }
        }
    }
}
