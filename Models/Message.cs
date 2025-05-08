// Models/Message.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketplaceAPI.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ChatId { get; set; }
        [ForeignKey(nameof(ChatId))]
        public Chat Chat { get; set; }

        [Required]
        public string SenderId { get; set; }
        [ForeignKey(nameof(SenderId))]
        public ApplicationUser Sender { get; set; }

        [Required, MaxLength(1000)]
        public string Text { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}