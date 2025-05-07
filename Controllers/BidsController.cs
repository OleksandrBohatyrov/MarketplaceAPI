using System;
using System.Linq;
using System.Security.Claims;
using MarketplaceAPI.Data;
using MarketplaceAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class BidsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public BidsController(ApplicationDbContext db) => _db = db;

    public class BidDto
    {
        public int ProductId { get; set; }
        public decimal Amount { get; set; }
    }

    // POST /api/bids
    [HttpPost]
    [Authorize]
    public IActionResult PlaceBid([FromBody] BidDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var product = _db.Products.Find(dto.ProductId);
        if (product == null)
            return NotFound("Product not found");
        if (!product.IsAuction)
            return BadRequest("Not an auction");
        if (product.EndsAt <= DateTime.UtcNow)
            return BadRequest("Auction ended");

        // --- Считаем текущий максимум ставки: либо из БД, либо минимальная при старте аукциона ---
        // EF Core корректно переведёт этот запрос в SQL
        var maxBidInDb = _db.Bids
            .Where(b => b.ProductId == dto.ProductId)
            .Select(b => (decimal?)b.Amount)
            .Max();

        // Если в БД нет ставок — берём минимальную
        var currentMax = maxBidInDb ?? (product.MinBid ?? 0);

        if (dto.Amount <= currentMax)
            return BadRequest($"Bid must exceed {currentMax}");

        var bid = new Bid
        {
            ProductId = dto.ProductId,
            BidderId = userId,
            Amount = dto.Amount
        };

        _db.Bids.Add(bid);
        _db.SaveChanges();

        return Ok(new
        {
            bid.Id,
            bid.Amount,
            bid.Timestamp,
            Bidder = new { Id = userId } // или можно сразу вернуть полную DTO
        });
    }

    // GET /api/bids?productId=123
    [HttpGet]
    public IActionResult GetBids([FromQuery] int productId)
    {
        var list = _db.Bids
            .Where(b => b.ProductId == productId)
            .Include(b => b.Bidder)
            .OrderByDescending(b => b.Amount)
            .Select(b => new {
                b.Id,
                b.Amount,
                b.Timestamp,
                Bidder = new { b.Bidder.Id, b.Bidder.UserName }
            })
            .ToList();

        return Ok(list);
    }
}
