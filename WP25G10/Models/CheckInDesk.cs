using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace WP25G10.Models
{
    public class CheckInDesk
    {
        public int Id { get; set; }

        [Required]
        public string Terminal { get; set; } = string.Empty;

        [Required]
        public int DeskNumber { get; set; }

        public bool IsActive { get; set; } = true;

        [Required]
        public string CreatedByUserId { get; set; } = string.Empty;

        [ForeignKey(nameof(CreatedByUserId))]
        public IdentityUser? CreatedByUser { get; set; }
    }
}
