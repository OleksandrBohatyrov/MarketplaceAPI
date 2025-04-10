using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace MarketplaceAPI.Models
{
    public class ApplicationUser : IdentityUser
    {
        [MaxLength(150)]
        public string FullName { get; set; }
    }
}
