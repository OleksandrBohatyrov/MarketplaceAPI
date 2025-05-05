using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketplaceAPI.Models
{
    public enum OrderStatus
    {
        PendingAddress,    
        AwaitingShipment,  
        Shipped,           
        Delivered,        
        Canceled           
    }

    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string BuyerId { get; set; }

        [ForeignKey(nameof(BuyerId))]
        public ApplicationUser Buyer { get; set; }

        [Required]
        public string SellerId { get; set; }

        [ForeignKey(nameof(SellerId))]
        public ApplicationUser Seller { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.PendingAddress;

        public string ShippingAddress { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ShippedAt { get; set; }

        public DateTime? DeliveredAt { get; set; }
    }
}
