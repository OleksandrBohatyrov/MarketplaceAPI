using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MarketplaceAPI.Models
{
    public class Transaction
    {
        [Key]
        public int Id { get; set; }
        public int OrderId { get; set; }
        [Required]
        public decimal Amount { get; set; }
        public string Status { get; set; } 
        [ForeignKey("OrderId")]
        public Order Order { get; set; }
    }
}
