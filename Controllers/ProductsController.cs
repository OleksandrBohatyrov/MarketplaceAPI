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
    [AllowAnonymous]
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
            var p = _db.Products
                .Include(x => x.Category)
                .Include(x => x.Seller)
                .Include(x => x.ProductTags)
                    .ThenInclude(pt => pt.Tag)
                .FirstOrDefault(x => x.Id == id);

            if (p == null)
                return NotFound(new { message = "Item not found" });

            var dto = new ProductResponseDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Category = new CategoryDto { Id = p.Category.Id, Name = p.Category.Name },
                Seller = new UserDto { Id = p.Seller.Id, UserName = p.Seller.UserName },
                Tags = p.ProductTags
                        .Select(pt => new TagDto { Id = pt.Tag.Id, Name = pt.Tag.Name })
                        .ToList(),
                CreatedAt = p.CreatedAt
            };

            return Ok(dto);
        }


        [HttpPost]
        [Authorize]
        public IActionResult CreateProduct([FromBody] ProductDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { message = "Incorrect product data" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();


            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                CategoryId = dto.CategoryId,
                SellerId = userId.ToString(),
                CreatedAt = DateTime.UtcNow
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
    public class ProductDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int CategoryId { get; set; }
    }
}
}
