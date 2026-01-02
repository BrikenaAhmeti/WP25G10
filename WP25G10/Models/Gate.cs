using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace WP25G10.Models
{
    public class Gate
    {
        public int Id { get; set; }

        [Required]
        public string Code { get; set; } = string.Empty;

        [Required]
        public string Terminal { get; set; } = string.Empty;

        [Required]
        public GateStatus Status { get; set; } = GateStatus.Open;

        public bool IsActive { get; set; } = true;

        [ValidateNever]
        public string CreatedByUserId { get; set; } = string.Empty;

        [ForeignKey(nameof(CreatedByUserId))]
        public IdentityUser? CreatedByUser { get; set; }

        public List<Flight> Flights { get; set; } = new();
    }
}
