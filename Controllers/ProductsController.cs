using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using MarketplaceAPI.Data;
using MarketplaceAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public ProductsController(ApplicationDbContext db) => _db = db;

        [HttpGet("feed")]
        public IActionResult GetFeed()
        {
            var products = _db.Products
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .Include(p => p.ProductTags)
                    .ThenInclude(pt => pt.Tag)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new {
                    p.Id,
                    p.Name,
                    p.Price,
                    p.CategoryId,
                    // Название категории, если надо
                    CategoryName = p.Category.Name,
                    // Список тегов
                    Tags = p.ProductTags
                             .Select(pt => new { pt.Tag.Id, pt.Tag.Name })
                             .ToList()
                })
                .ToList();

            return Ok(products);
        }

        // GET /api/products/{id}
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

            // Проекция в анонимный объект
            var result = new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                Category = new { p.Category.Id, p.Category.Name },
                SellerId = p.SellerId,
                Tags = p.ProductTags
                              .Select(pt => new { pt.Tag.Id, pt.Tag.Name })
                              .ToList(),
                p.CreatedAt
            };

            return Ok(result);
        }

        // POST /api/products
        [HttpPost]
        [Authorize]
        public IActionResult CreateProduct([FromBody] ProductDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { message = "Incorrect product data" });

            if (dto.TagIds?.Count > 5)
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
            if (dto.TagIds != null && dto.TagIds.Any())
            {
                foreach (var tagId in dto.TagIds.Take(5))
                {
                    _db.ProductTags.Add(new ProductTag
                    {
                        ProductId = product.Id,
                        TagId = tagId
                    });
                }
                _db.SaveChanges();
            }

            return Ok(product);
        }

        // PUT /api/products/{id}
        [HttpPut("{id}")]
        [Authorize]
        public IActionResult UpdateProduct(int id, [FromBody] ProductDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { message = "Incorrect product data" });

            if (dto.TagIds?.Count > 5)
                return BadRequest(new { message = "Максимум 5 тегов" });

            var product = _db.Products
                             .Include(p => p.ProductTags)
                             .FirstOrDefault(p => p.Id == id);
            if (product == null)
                return NotFound(new { message = "Item not found" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            if (product.SellerId != userId && !isAdmin)
                return Forbid();

            // обновляем поля
            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.CategoryId = dto.CategoryId;

            // удаляем старые теги
            if (product.ProductTags.Any())
            {
                _db.ProductTags.RemoveRange(product.ProductTags);
                _db.SaveChanges();
            }

            // добавляем новые
            if (dto.TagIds != null && dto.TagIds.Any())
            {
                foreach (var tagId in dto.TagIds.Take(5))
                {
                    _db.ProductTags.Add(new ProductTag
                    {
                        ProductId = product.Id,
                        TagId = tagId
                    });
                }
                _db.SaveChanges();
            }

            _db.Products.Update(product);
            _db.SaveChanges();

            return Ok(product);
        }

        // DELETE /api/products/{id}
        [HttpDelete("{id}")]
        [Authorize]
        public IActionResult DeleteProduct(int id)
        {
            var product = _db.Products.FirstOrDefault(p => p.Id == id);
            if (product == null)
                return NotFound(new { message = "Item not found" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            if (product.SellerId != userId && !isAdmin)
                return Forbid();

            _db.Products.Remove(product);
            _db.SaveChanges();

            return Ok(new { message = "The item has been successfully removed" });
        }

        // DTO для Create/Update
        public class ProductDto
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public decimal Price { get; set; }
            public int CategoryId { get; set; }
            public List<int> TagIds { get; set; } = new List<int>();
        }
    }
}
