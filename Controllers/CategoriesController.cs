using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MarketplaceAPI.Data;
using MarketplaceAPI.Models;
using System.Linq;

namespace MarketplaceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public CategoriesController(ApplicationDbContext db)
            => _db = db;

        // get all categories
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetAll()
        {
            var cats = _db.Categories
                          .OrderBy(c => c.Name)
                          .ToList();
            return Ok(cats);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] Category model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _db.Categories.Add(model);
            await _db.SaveChangesAsync();
            return Ok(model);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] Category model)
        {
            var existing = _db.Categories.Find(id);
            if (existing == null)
                return NotFound();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            existing.Name = model.Name;
            await _db.SaveChangesAsync();
            return Ok(existing);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = _db.Categories.Find(id);
            if (existing == null)
                return NotFound();

            _db.Categories.Remove(existing);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Deleted" });
        }
    }
}
