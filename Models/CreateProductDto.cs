using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MarketplaceAPI.Models
{
    public class CreateProductDto
    {
        [Required, MaxLength(200)]
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int CategoryId { get; set; }
        public List<int> TagIds { get; set; } = new List<int>();
      
        public List<IFormFile> Images { get; set; }

        public bool IsAuction { get; set; }
        public decimal? MinBid { get; set; }
        public DateTime? EndsAt { get; set; }
    }
}
