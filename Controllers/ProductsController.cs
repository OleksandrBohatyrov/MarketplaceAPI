using MarketplaceAPI.Data;
using MarketplaceAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public ProductsController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("feed")]
        public IActionResult GetFeed()
        {
            var products = _db.Products
                              .Include(p => p.Category)
                              .Include(p => p.Seller)
                              .OrderByDescending(p => p.CreatedAt)
                              .ToList();
            return Ok(products);
        }


        [HttpGet("{id}")]
        public IActionResult GetProduct(int id)
        {
            var product = _db.Products
                             .Include(p => p.Category)
                             .Include(p => p.Seller)
                             .FirstOrDefault(p => p.Id == id);
            if (product == null)
                return NotFound(new { message = "Товар не найден" });

            return Ok(product);
        }


        [HttpPost]
        public IActionResult CreateProduct([FromBody] CreateProductDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Получаем текущего пользователя
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                CategoryId = dto.CategoryId,
                SellerId = userId  
            };

            _db.Products.Add(product);
            _db.SaveChanges();
            return Ok(product);
        }


        [HttpPut("{id}")]
        [Authorize]
        public IActionResult UpdateProduct(int id, [FromBody] Product updatedProduct)
        {
            var existingProduct = _db.Products.FirstOrDefault(p => p.Id == id);
            if (existingProduct == null)
                return NotFound(new { message = "Item not found" });

            // check if user is a product owner or admin
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            if (existingProduct.SellerId != currentUserId && !isAdmin)
                return Forbid();

            existingProduct.Name = updatedProduct.Name;
            existingProduct.Description = updatedProduct.Description;
            existingProduct.Price = updatedProduct.Price;
            existingProduct.CategoryId = updatedProduct.CategoryId;

            _db.Products.Update(existingProduct);
            _db.SaveChanges();

            return Ok(existingProduct);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public IActionResult DeleteProduct(int id)
        {
            var product = _db.Products.FirstOrDefault(p => p.Id == id);
            if (product == null)
                return NotFound(new { message = "Item not found" });

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            if (product.SellerId != currentUserId && !isAdmin)
                return Forbid();

            _db.Products.Remove(product);
            _db.SaveChanges();

            return Ok(new { message = "The item has been successfully removed" });
        }
    }
}
