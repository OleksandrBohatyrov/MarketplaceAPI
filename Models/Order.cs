using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketplaceAPI.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        public string BuyerId { get; set; }

        public int ProductId { get; set; }

        [Required]
        public DateTime OrderDate { get; set; }

        [ForeignKey("BuyerId")]
        public ApplicationUser Buyer { get; set; }

        [ForeignKey("ProductId")]
        public Product Product { get; set; }
    }
}
