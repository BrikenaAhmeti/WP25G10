using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace WP25G10.Models
{
    public class Airline
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(5)]
        public string Code { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Country { get; set; }

        [Display(Name = "Logo URL")]
        public string? LogoUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public string? CreatedByUserId { get; set; } = default!;

        [ForeignKey(nameof(CreatedByUserId))]
        public IdentityUser? CreatedByUser { get; set; }

        public List<Flight> Flights { get; set; } = new();
    }
}