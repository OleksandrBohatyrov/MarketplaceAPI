using Microsoft.AspNetCore.Mvc;

namespace FinalProject_Back_end.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetAllProducts() => Ok("All products");

        [HttpGet("{id}")]
        public IActionResult GetProduct(int id) => Ok($"Product {id}");

        [HttpPost]
        public IActionResult CreateProduct() => Ok("Create product");

        [HttpPut("{id}")]
        public IActionResult UpdateProduct(int id) => Ok($"Update product {id}");

        [HttpDelete("{id}")]
        public IActionResult DeleteProduct(int id) => Ok($"Delete product {id}");
    }
}
