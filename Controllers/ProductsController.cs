// File: Controllers/ProductsController.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using MarketplaceAPI.Data;
using MarketplaceAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace MarketplaceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IAmazonS3 _s3;
        private readonly string _bucketName;

        public ProductsController(
            ApplicationDbContext db,
            IAmazonS3 s3Client,
            IConfiguration config)
        {
            _db = db;
            _s3 = s3Client;
            _bucketName = config["AWS:BucketName"];
        }

        [HttpGet("feed")]
        [AllowAnonymous]
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
                    CategoryName = p.Category.Name,
                    ImageUrl = p.ImageUrl,
                    Tags = p.ProductTags
                             .Select(pt => new { pt.Tag.Id, pt.Tag.Name })
                             .ToList()
                })
                .ToList();

            return Ok(products);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
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

            var result = new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                Category = new { p.Category.Id, p.Category.Name },
                p.ImageUrl,
                SellerId = p.SellerId,
                Tags = p.ProductTags
                              .Select(pt => new { pt.Tag.Id, pt.Tag.Name })
                              .ToList(),
                p.CreatedAt
            };

            return Ok(result);
        }

        [HttpPost]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateProduct([FromForm] ProductDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { message = "Incorrect product data" });

            if (dto.TagIds?.Count > 5)
                return BadRequest(new { message = "Max 5 tags" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            string imageUrl = null;
            if (dto.Image != null && dto.Image.Length > 0)
            {
                var key = $"{Guid.NewGuid()}_{dto.Image.FileName}";
                using var ms = new MemoryStream();
                await dto.Image.CopyToAsync(ms);

                var putRequest = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = key,
                    InputStream = ms,
                };
                await _s3.PutObjectAsync(putRequest);

                imageUrl = $"https://{_bucketName}.s3.{_s3.Config.RegionEndpoint.SystemName}.amazonaws.com/{key}";
            }

            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                CategoryId = dto.CategoryId,
                SellerId = userId,
                CreatedAt = DateTime.UtcNow,
                ImageUrl = imageUrl
            };

            _db.Products.Add(product);
            await _db.SaveChangesAsync();

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
                await _db.SaveChangesAsync();
            }

            return Ok(product);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { message = "Incorrect product data" });

            if (dto.TagIds?.Count > 5)
                return BadRequest(new { message = "Max 5 tags" });

            var product = await _db.Products
                                   .Include(p => p.ProductTags)
                                   .FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
                return NotFound(new { message = "Item not found" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            if (product.SellerId != userId && !isAdmin)
                return Forbid();

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.CategoryId = dto.CategoryId;

            if (product.ProductTags.Any())
                _db.ProductTags.RemoveRange(product.ProductTags);

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
            }

            _db.Products.Update(product);
            await _db.SaveChangesAsync();

            return Ok(product);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null)
                return NotFound(new { message = "Item not found" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            if (product.SellerId != userId && !isAdmin)
                return Forbid();

            _db.Products.Remove(product);
            await _db.SaveChangesAsync();

            return Ok(new { message = "The item has been successfully removed" });
        }

        public class ProductDto
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public decimal Price { get; set; }
            public int CategoryId { get; set; }
            public List<int> TagIds { get; set; } = new List<int>();
            public IFormFile Image { get; set; }
        }
    }
}
