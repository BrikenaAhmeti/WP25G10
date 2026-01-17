using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace WP25G10.Models
{
    public class FlightFavorite
    {
        public int Id { get; set; }

        [Required]
        public int FlightId { get; set; }

        [ForeignKey(nameof(FlightId))]
        public Flight? Flight { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public IdentityUser? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
