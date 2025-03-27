using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MarketplaceAPI.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }
        public int BuyerId { get; set; }
        public int ProductId { get; set; }
        [Required]
        public DateTime OrderDate { get; set; }
        [ForeignKey("BuyerId")]
        public User Buyer { get; set; }
        [ForeignKey("ProductId")]
        public Product Product { get; set; }
    }
}
