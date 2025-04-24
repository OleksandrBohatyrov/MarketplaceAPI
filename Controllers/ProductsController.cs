using MarketplaceAPI.Data;
using MarketplaceAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;

namespace MarketplaceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public ProductsController(ApplicationDbContext db) => _db = db;

        // GET feed с тегами
        [HttpGet("feed")]
        public IActionResult GetFeed()
        {
            var products = _db.Products
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .Include(p => p.ProductTags)
                    .ThenInclude(pt => pt.Tag)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            return Ok(products);
        }

        // GET одного товара с тегами
        [HttpGet("{id}")]
        public IActionResult GetProduct(int id)
        {
            var product = _db.Products
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .Include(p => p.ProductTags)
                    .ThenInclude(pt => pt.Tag)
                .FirstOrDefault(p => p.Id == id);

            if (product == null)
                return NotFound(new { message = "Item not found" });

            return Ok(product);
        }

        // POST — создание с тегами
        [HttpPost]
        [Authorize]
        public IActionResult CreateProduct([FromBody] CreateProductDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.TagIds.Count > 5)
                return BadRequest(new { message = "Максимум 5 тегов" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                CategoryId = dto.CategoryId,
                SellerId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _db.Products.Add(product);
            _db.SaveChanges();

            // сохраняем теги
            foreach (var tagId in dto.TagIds.Take(5))
            {
                _db.ProductTags.Add(new ProductTag
                {
                    ProductId = product.Id,
                    TagId = tagId
                });
            }
            _db.SaveChanges();

            return Ok(product);
        }

        // PUT — обновление, включая теги
        [HttpPut("{id}")]
        [Authorize]
        public IActionResult UpdateProduct(int id, [FromBody] CreateProductDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.TagIds.Count > 5)
                return BadRequest(new { message = "Максимум 5 тегов" });

            var existing = _db.Products
                .Include(p => p.ProductTags)
                .FirstOrDefault(p => p.Id == id);

            if (existing == null)
                return NotFound(new { message = "Item not found" });

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            if (existing.SellerId != currentUserId && !isAdmin)
                return Forbid();

            // обновляем поля
            existing.Name = dto.Name;
            existing.Description = dto.Description;
            existing.Price = dto.Price;
            existing.CategoryId = dto.CategoryId;

            // обновляем теги: сначала чистим старые
            _db.ProductTags.RemoveRange(existing.ProductTags);
            _db.SaveChanges();

            // и добавляем новые
            foreach (var tagId in dto.TagIds.Take(5))
            {
                _db.ProductTags.Add(new ProductTag
                {
                    ProductId = existing.Id,
                    TagId = tagId
                });
            }

            _db.Products.Update(existing);
            _db.SaveChanges();

            return Ok(existing);
        }

        // DELETE без изменений
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
