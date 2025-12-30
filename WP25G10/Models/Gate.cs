using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace WP25G10.Models
{
    public class Gate
    {
        public int Id { get; set; }

        [Required]
        public string Code { get; set; } = string.Empty; // e.g. A1

        [Required]
        public string Terminal { get; set; } = string.Empty; // e.g. T1

        [Required]
        public GateStatus Status { get; set; } = GateStatus.Open;

        public bool IsActive { get; set; } = true;

        [Required]
        public string CreatedByUserId { get; set; } = string.Empty;

        [ForeignKey(nameof(CreatedByUserId))]
        public IdentityUser? CreatedByUser { get; set; }

        public List<Flight> Flights { get; set; } = new();
    }
}
