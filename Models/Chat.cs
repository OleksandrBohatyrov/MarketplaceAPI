using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketplaceAPI.Models
{
    public class Chat
    {
        [Key]
        public int Id { get; set; }

        // участники
        [Required]
        public string UserAId { get; set; }
        [ForeignKey(nameof(UserAId))]
        public ApplicationUser UserA { get; set; }

        [Required]
        public string UserBId { get; set; }
        [ForeignKey(nameof(UserBId))]
        public ApplicationUser UserB { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}