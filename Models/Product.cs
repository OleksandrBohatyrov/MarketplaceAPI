using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        public ProductStatus Status { get; set; } = ProductStatus.Available;

        public bool IsAuction { get; set; } = false;
        public decimal? MinBid { get; set; }
        public DateTime? EndsAt { get; set; }

        public ICollection<ProductImage> ProductImages { get; set; }
            = new List<ProductImage>();

        public ICollection<ProductTag> ProductTags { get; set; }
            = new List<ProductTag>();

        public ICollection<Bid> Bids { get; set; }
            = new List<Bid>();
    }
}
