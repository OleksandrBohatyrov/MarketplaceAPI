using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketplaceAPI.Models
{
    public enum TradeStatus
    {
        Pending,
        Accepted,
        Rejected
    }

    public class Trade
    {
        [Key]
        public int Id { get; set; }

      
        public string RequesterId { get; set; }
        [ForeignKey(nameof(RequesterId))]
        public ApplicationUser Requester { get; set; }

       
        public int OfferedProductId { get; set; }
        [ForeignKey(nameof(OfferedProductId))]
        public Product OfferedProduct { get; set; }

      
        public int RequestedProductId { get; set; }
        [ForeignKey(nameof(RequestedProductId))]
        public Product RequestedProduct { get; set; }

        public TradeStatus Status { get; set; } = TradeStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
