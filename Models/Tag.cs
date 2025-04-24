using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MarketplaceAPI.Models
{
    public class Tag
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        public ICollection<ProductTag> ProductTags { get; set; }
            = new List<ProductTag>();
    }
}