// Models/CreateProductDto.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MarketplaceAPI.Models
{
    public class CreateProductDto
    {
        [Required, MaxLength(200)]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        public int CategoryId { get; set; }

  
        public List<int> TagIds { get; set; } = new List<int>();
    }
}
