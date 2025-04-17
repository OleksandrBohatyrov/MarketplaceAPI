using System.ComponentModel.DataAnnotations;

namespace MarketplaceAPI.Models
{
    public class Category
    {
        [Key]//вфывфыывczczc
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
    }
}
