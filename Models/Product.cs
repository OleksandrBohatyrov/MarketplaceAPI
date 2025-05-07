using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;

namespace MarketplaceAPI.Models
{
    public enum ProductStatus
    {
        Available,
        Sold
    }

    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        public decimal Price { get; set; }


        public string SellerId { get; set; }

        [ForeignKey(nameof(SellerId))]
        public ApplicationUser Seller { get; set; }

        public int CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public Category Category { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ProductTag> ProductTags { get; set; }
            = new List<ProductTag>();

        public string ImageUrl { get; set; }

        // теперь EF хранит этот enum как INT (0 = Available, 1 = Sold)
        public ProductStatus Status { get; set; } = ProductStatus.Available;

        
        public bool IsAuction { get; set; } = false;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinBid { get; set; }

      
        public DateTime? EndsAt { get; set; }

        
        public ICollection<Bid> Bids { get; set; }
            = new List<Bid>();
    }
}
