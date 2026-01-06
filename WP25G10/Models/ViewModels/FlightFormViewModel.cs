using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using WP25G10.Models;

namespace WP25G10.Models.ViewModels
{
    public class FlightFormViewModel
    {
        public int? Id { get; set; }

        [Required, StringLength(20)]
        public string FlightNumber { get; set; } = string.Empty;

        [Required]
        public int AirlineId { get; set; }

        [Required]
        public int GateId { get; set; }

        public int? CheckInDeskId { get; set; }

        [Required]
        public string OriginAirport { get; set; } = string.Empty;

        [Required]
        public string DestinationAirport { get; set; } = string.Empty;

        [Required]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
        public DateTime DepartureTime { get; set; }

        [Required]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
        public DateTime ArrivalTime { get; set; }

        [Required]
        public FlightStatus Status { get; set; } = FlightStatus.Scheduled;

        [Range(0, 24 * 60)]
        public int DelayMinutes { get; set; } = 0;

        public List<SelectListItem> Airlines { get; set; } = new();
        public List<SelectListItem> Gates { get; set; } = new();
        public List<SelectListItem> CheckInDesks { get; set; } = new();
    }
}
