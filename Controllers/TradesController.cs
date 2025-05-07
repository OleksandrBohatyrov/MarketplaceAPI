// File: Controllers/TradesController.cs

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MarketplaceAPI.Data;
using MarketplaceAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TradesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public TradesController(ApplicationDbContext db) => _db = db;

        public class TradeDto
        {
            public int TargetProductId { get; set; }
            public int OfferedProductId { get; set; }
        }

        // POST /api/trades
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Propose([FromBody] TradeDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Загружаем оба товара
            var target = await _db.Products.FindAsync(dto.TargetProductId);
            var offered = await _db.Products.FindAsync(dto.OfferedProductId);

            if (target == null || offered == null)
                return NotFound("Product not found");

            // Только доступные товары
            if (target.Status != ProductStatus.Available
             || offered.Status != ProductStatus.Available)
                return BadRequest("Cannot trade sold items");

            // Нельзя предлагать обмен на свой же товар
            if (target.SellerId == userId)
                return BadRequest("Invalid trade: you cannot propose a trade on your own item");

            // Нельзя предлагать чужой товар — только тот, который принадлежит вам
            if (offered.SellerId != userId)
                return BadRequest("Invalid trade: you can only offer your own items");

            // Всё ок, создаём предложение
            var trade = new Trade
            {
                TargetProductId = dto.TargetProductId,
                OfferedProductId = dto.OfferedProductId,
                ProposerId = userId,
                Status = TradeStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _db.Trades.Add(trade);
            await _db.SaveChangesAsync();

            return Ok(new { trade.Id });
        }

        // GET /api/trades/incoming
        [HttpGet("incoming")]
        [Authorize]
        public async Task<IActionResult> Incoming()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var list = await _db.Trades
                .Where(t => t.TargetProduct.SellerId == userId)
                .Include(t => t.Proposer)
                .Include(t => t.OfferedProduct).ThenInclude(p => p.Category)
                .Include(t => t.TargetProduct).ThenInclude(p => p.Category)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new {
                    t.Id,
                    t.Status,
                    t.CreatedAt,
                    Offered = new
                    {
                        t.OfferedProduct.Id,
                        t.OfferedProduct.Name,
                        Category = t.OfferedProduct.Category.Name,
                        t.OfferedProduct.ProductImages
                    },
                    Target = new
                    {
                        t.TargetProduct.Id,
                        t.TargetProduct.Name,
                        Category = t.TargetProduct.Category.Name
                    },
                    Proposer = new
                    {
                        t.Proposer.Id,
                        t.Proposer.UserName,
                        t.Proposer.Email
                    }
                })
                .ToListAsync();
            return Ok(list);
        }

        // POST /api/trades/{id}/accept
        [HttpPost("{id:int}/accept")]
        [Authorize]
        public async Task<IActionResult> Accept(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var trade = await _db.Trades
                .Include(t => t.OfferedProduct)
                .Include(t => t.TargetProduct)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (trade == null)
                return NotFound();

            if (trade.TargetProduct.SellerId != userId)
                return Forbid();

            trade.TargetProduct.Status = ProductStatus.Sold;
            trade.OfferedProduct.Status = ProductStatus.Sold;
            trade.Status = TradeStatus.Accepted;

            await _db.SaveChangesAsync();
            return Ok();
        }

        // POST /api/trades/{id}/reject
        [HttpPost("{id:int}/reject")]
        [Authorize]
        public async Task<IActionResult> Reject(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var trade = await _db.Trades
                .Include(t => t.TargetProduct)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (trade == null)
                return NotFound();

            if (trade.TargetProduct.SellerId != userId)
                return Forbid();

            trade.Status = TradeStatus.Rejected;
            await _db.SaveChangesAsync();
            return Ok();
        }
    }
}
