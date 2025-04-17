using MarketplaceAPI.Data;
using MarketplaceAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
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

        public CartController(ApplicationDbContext db)
        {
            _db = db;
        }

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
                                     Product = new
                                     {
                                         ci.Product.Id,
                                         ci.Product.Name,
                                         ci.Product.Price,
                                         ci.Product.Description
                                     }
                                 })
                                 .ToListAsync();
            return Ok(items);
        }

        // POST /api/cart/add/5
        [HttpPost("add/{productId}")]
        public async Task<IActionResult> AddToCart(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var product = await _db.Products.FindAsync(productId);
            if (product == null)
                return NotFound(new { message = "Product not found" });

            // нельзя добавлять свой товар
            if (product.SellerId.ToString() == userId)
                return BadRequest(new { message = "Нельзя добавить свой товар в корзину" });

            var existing = await _db.CartItems
                                    .FirstOrDefaultAsync(ci => ci.UserId == userId && ci.ProductId == productId);
            if (existing != null)
            {
                existing.Quantity++;
            }
            else
            {
                _db.CartItems.Add(new CartItem
                {
                    UserId = userId,
                    ProductId = productId,
                    Quantity = 1
                });
            }

            await _db.SaveChangesAsync();
            return Ok(new { message = "Добавлено в корзину" });
        }
    }
}
