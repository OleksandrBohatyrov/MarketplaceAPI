using MarketplaceAPI.Data;
using MarketplaceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class TagsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public TagsController(ApplicationDbContext db) => _db = db;

    // GET /api/tags
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tags = await _db.Tags
            .Select(t => new TagWithCountDto
            {
                Id = t.Id,
                Name = t.Name,
                ProductCount = t.ProductTags.Count
            })
            .OrderByDescending(t => t.ProductCount)
            .ToListAsync();

        return Ok(tags);
    }

    // POST /api/tags
    [HttpPost, Route("")]
    public async Task<IActionResult> Create([FromBody] TagDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Name is required");

        var name = dto.Name.Trim().ToLower();
        var existing = await _db.Tags
            .FirstOrDefaultAsync(t => t.Name.ToLower() == name);

        if (existing != null)
        {
            return Ok(new TagDto { Id = existing.Id, Name = existing.Name });
        }

        var tag = new Tag { Name = dto.Name.Trim() };
        _db.Tags.Add(tag);
        await _db.SaveChangesAsync();

        return Ok(new TagDto { Id = tag.Id, Name = tag.Name });
    }
}
