using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace WP25G10.Models
{
    public class Flight
    {
        public int Id { get; set; }

        [Required, StringLength(20)]
        public string FlightNumber { get; set; } = string.Empty;

        [Required]
        public int AirlineId { get; set; }
        public Airline? Airline { get; set; }

        [Required]
        public int GateId { get; set; }
        public Gate? Gate { get; set; }

        [Required]
        public string OriginAirport { get; set; } = string.Empty;

        [Required]
        public string DestinationAirport { get; set; } = string.Empty;

        [Required]
        public DateTime DepartureTime { get; set; }

        [Required]
        public DateTime ArrivalTime { get; set; }

        [Required]
        public FlightStatus Status { get; set; } = FlightStatus.Scheduled;

        public bool IsActive { get; set; } = true;

        public string? CheckInTerminal { get; set; }
        public int? CheckInDeskFrom { get; set; }
        public int? CheckInDeskTo { get; set; }

        [Required]
        public string CreatedByUserId { get; set; } = string.Empty;

        [ForeignKey(nameof(CreatedByUserId))]
        public IdentityUser? CreatedByUser { get; set; }
    }
}
