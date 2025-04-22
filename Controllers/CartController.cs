// Controllers/CartController.cs
using MarketplaceAPI.Data;
using MarketplaceAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MarketplaceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public CartController(ApplicationDbContext db) => _db = db;

        // GET /api/cart
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var items = await _db.CartItems
                .Include(ci => ci.Product)
                .Where(ci => ci.UserId == userId)
                .Select(ci => new {
                    ci.Id,
                    ci.Quantity,
                    ProductId = ci.Product.Id,
                    ProductName = ci.Product.Name,
                    ci.Product.Price
                })
                .ToListAsync();
            return Ok(items);
        }

        // GET /api/cart/count
        [HttpGet("count")]
        public async Task<IActionResult> GetCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var total = await _db.CartItems
                .Where(ci => ci.UserId == userId)
                .SumAsync(ci => ci.Quantity);
            return Ok(new { count = total });
        }

        // POST /api/cart/add/{productId}
        [HttpPost("add/{productId}")]
        public async Task<IActionResult> AddToCart(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var product = await _db.Products.FindAsync(productId);
            if (product == null) return NotFound("Product not found");
            if (product.SellerId.ToString() == userId)
                return BadRequest("You can't add your item to the basket");

            var existing = await _db.CartItems
                .FirstOrDefaultAsync(ci => ci.UserId == userId && ci.ProductId == productId);

            if (existing != null)
                existing.Quantity++;
            else
                _db.CartItems.Add(new CartItem
                {
                    UserId = userId,
                    ProductId = productId,
                    Quantity = 1
                });

            await _db.SaveChangesAsync();
            return Ok("Added to cart");
        }

        // DELETE /api/cart/{cartItemId}
        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var item = await _db.CartItems.FindAsync(id);
            if (item == null || item.UserId != userId)
                return NotFound();

            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();
            return Ok("Removed form cart");
        }
    }
}
