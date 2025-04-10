using Microsoft.AspNetCore.Mvc;
using MarketplaceAPI.Models;
using MarketplaceAPI.Data;
using System.Linq;

namespace MarketplaceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
                              .OrderByDescending(p => p.CreatedAt)
                              .ToList();

            return Ok(products);
        }

  
        [HttpGet("{id}")]
        public IActionResult GetProduct(int id)
        {
            var product = _db.Products.FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                return NotFound(new { message = "Товар не найден" });
            }

            return Ok(product);
        }


        [HttpPost]
        public IActionResult CreateProduct([FromBody] Product product)
        {

            if (product == null)
            {
                return BadRequest(new { message = "Некорректные данные товара" });
            }

            _db.Products.Add(product);
            _db.SaveChanges();

            return Ok(product);
        }

  
        [HttpPut("{id}")]
        public IActionResult UpdateProduct(int id, [FromBody] Product updatedProduct)
        {
            var existingProduct = _db.Products.FirstOrDefault(p => p.Id == id);
            if (existingProduct == null)
            {
                return NotFound(new { message = "Товар не найден" });
            }

            existingProduct.Name = updatedProduct.Name;
            existingProduct.Description = updatedProduct.Description;
            existingProduct.Price = updatedProduct.Price;
            existingProduct.CategoryId = updatedProduct.CategoryId;


            _db.Products.Update(existingProduct);
            _db.SaveChanges();

            return Ok(existingProduct);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteProduct(int id)
        {
            var product = _db.Products.FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                return NotFound(new { message = "Товар не найден" });
            }

            _db.Products.Remove(product);
            _db.SaveChanges();

            return Ok(new { message = "Товар успешно удалён" });
        }
    }
}
