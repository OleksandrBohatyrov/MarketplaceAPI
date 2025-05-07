// File: Models/Trade.cs
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

        public int TargetProductId { get; set; }
        [ForeignKey(nameof(TargetProductId))]
        public Product TargetProduct { get; set; }

        public int OfferedProductId { get; set; }
        [ForeignKey(nameof(OfferedProductId))]
        public Product OfferedProduct { get; set; }

        public string ProposerId { get; set; }
        [ForeignKey(nameof(ProposerId))]
        public ApplicationUser Proposer { get; set; }

        public TradeStatus Status { get; set; } = TradeStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
