// File: Controllers/ProductsController.cs

using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Amazon.S3;
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


        private void FinalizeAuctions()
        {
            var now = DateTime.UtcNow;
            var expired = _db.Products
                .Where(p => p.IsAuction && p.EndsAt <= now && p.Status == ProductStatus.Available)
                .ToList();
            if (expired.Any())
            {
                expired.ForEach(p => p.Status = ProductStatus.Sold);
                _db.SaveChanges();
            }
        }


        [HttpGet("feed"), AllowAnonymous]
        public IActionResult GetFeed()
        {
            FinalizeAuctions();
            var now = DateTime.UtcNow;

            var products = _db.Products
                .Where(p => p.Status == ProductStatus.Available)
                .Where(p => !p.IsAuction || (p.IsAuction && p.EndsAt > now))
                .Include(p => p.Category)
                .Include(p => p.ProductTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.ProductImages)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new {
                    p.Id,
                    p.Name,
                    p.Price,
                    p.IsAuction,
                    MinBid = p.MinBid,
                    EndsAt = p.EndsAt,
                    Status = p.Status.ToString(),
                    Category = new { p.Category.Id, p.Category.Name },
                    ImageUrls = p.ProductImages.Select(pi => pi.Url),
                    Tags = p.ProductTags.Select(pt => new { pt.Tag.Id, pt.Tag.Name })
                })
                .ToList();

            return Ok(products);
        }


        [HttpGet("{id:int}"), AllowAnonymous]
        public IActionResult GetProduct(int id)
        {
            FinalizeAuctions();

            var p = _db.Products
                .Include(p => p.Category)
                .Include(p => p.ProductTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.ProductImages)
                .FirstOrDefault(p => p.Id == id);

            if (p == null)
                return NotFound(new { message = "Item not found" });

            return Ok(new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.IsAuction,
                MinBid = p.MinBid,
                EndsAt = p.EndsAt,
                Status = p.Status.ToString(),
                Category = new { p.Category.Id, p.Category.Name },
                ImageUrls = p.ProductImages.Select(pi => pi.Url),
                Tags = p.ProductTags.Select(pt => new { pt.Tag.Id, pt.Tag.Name }),
                p.CreatedAt
            });
        }

        [HttpGet("my-products"), Authorize]
        public IActionResult GetMyProducts()
        {
            FinalizeAuctions();
            var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var list = _db.Products
                .Where(p => p.SellerId == sellerId)
                .Include(p => p.Category)
                .Include(p => p.ProductTags).ThenInclude(pt => pt.Tag)
                .Include(p => p.ProductImages)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new {
                    p.Id,
                    p.Name,
                    p.Price,
                    p.IsAuction,
                    MinBid = p.MinBid,
                    EndsAt = p.EndsAt,
                    Status = p.Status.ToString(),
                    Category = new { p.Category.Id, p.Category.Name },
                    ImageUrls = p.ProductImages.Select(pi => pi.Url),
                    Tags = p.ProductTags.Select(pt => new { pt.Tag.Id, pt.Tag.Name })
                })
                .ToList();

            return Ok(list);
        }


        [HttpPost, Authorize, Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateProduct([FromForm] CreateProductDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Проверка картинок
            if (dto.Images == null || dto.Images.Count == 0 || dto.Images.Count > 4)
                return BadRequest(new { message = "Vali 1–4 pilti." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // 1) создаём продукт
            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                CategoryId = dto.CategoryId,
                SellerId = userId,
                CreatedAt = DateTime.UtcNow,
                Status = ProductStatus.Available,
                IsAuction = dto.IsAuction,
                MinBid = dto.IsAuction ? dto.MinBid : null,
                EndsAt = dto.IsAuction ? dto.EndsAt : null
            };
            _db.Products.Add(product);
            await _db.SaveChangesAsync();

            // 2) теги по именам (до 5)
            foreach (var raw in dto.TagNames
                                  .Select(n => n.Trim())
                                  .Where(n => !string.IsNullOrEmpty(n))
                                  .Distinct(StringComparer.OrdinalIgnoreCase)
                                  .Take(5))
            {
                var lower = raw.ToLower();
                var tag = await _db.Tags
                    .FirstOrDefaultAsync(t => t.Name.ToLower() == lower);

                if (tag == null)
                {
                    tag = new Tag { Name = raw };
                    _db.Tags.Add(tag);
                    await _db.SaveChangesAsync();
                }

                _db.ProductTags.Add(new ProductTag
                {
                    ProductId = product.Id,
                    TagId = tag.Id
                });
            }
            await _db.SaveChangesAsync();

            // 3) сохраняем изображения в S3 + БД
            foreach (var file in dto.Images)
            {
                var key = $"{Guid.NewGuid()}_{file.FileName}";
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                await _s3.PutObjectAsync(new Amazon.S3.Model.PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = key,
                    InputStream = ms
                });
                var url = $"https://{_bucketName}.s3.{_s3.Config.RegionEndpoint.SystemName}.amazonaws.com/{key}";

                _db.ProductImages.Add(new ProductImage
                {
                    ProductId = product.Id,
                    Url = url
                });
            }
            await _db.SaveChangesAsync();

            return Ok(new { product.Id });
        }


        [HttpPut("{id:int}"), Authorize]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            if (dto.TagIds.Count > 5)
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
            product.IsAuction = dto.IsAuction;
            product.MinBid = dto.IsAuction ? dto.MinBid : null;
            product.EndsAt = dto.IsAuction ? dto.EndsAt : null;

            _db.ProductTags.RemoveRange(product.ProductTags);
            foreach (var tid in dto.TagIds.Take(5))
            {
                _db.ProductTags.Add(new ProductTag
                {
                    ProductId = product.Id,
                    TagId = tid
                });
            }
            await _db.SaveChangesAsync();

            return Ok(product);
        }

 
        [HttpDelete("{id:int}"), Authorize]
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
    }


    public class CreateProductDto
    {
        [Required, MaxLength(200)]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        public int CategoryId { get; set; }

        public List<string> TagNames { get; set; } = new();

        [Required]
        public List<IFormFile> Images { get; set; }

        public bool IsAuction { get; set; }
        public decimal? MinBid { get; set; }
        public DateTime? EndsAt { get; set; }
    }


    public class UpdateProductDto
    {
        [Required, MaxLength(200)]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        public int CategoryId { get; set; }

        public List<int> TagIds { get; set; } = new();

        public bool IsAuction { get; set; }
        public decimal? MinBid { get; set; }
        public DateTime? EndsAt { get; set; }
    }
}
