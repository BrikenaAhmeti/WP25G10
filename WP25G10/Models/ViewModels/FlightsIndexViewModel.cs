using WP25G10.Models;

namespace WP25G10.Models.ViewModels
{
    public class FlightsIndexViewModel
    {
        public List<Flight> Flights { get; set; } = new();

        public string? SearchTerm { get; set; }
        public string StatusFilter { get; set; } = "all";

        public string Board { get; set; } = "departures";
        public int? AirlineId { get; set; }
        public int? GateId { get; set; }
        public string? Terminal { get; set; }
        public FlightStatus? FlightStatus { get; set; }
        public DateTime? Date { get; set; }
        public bool DelayedOnly { get; set; }

        public int PageNumber { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
