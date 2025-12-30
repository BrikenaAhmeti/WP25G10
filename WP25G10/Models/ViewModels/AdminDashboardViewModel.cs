namespace WP25G10.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalAirlines { get; set; }
        public int ActiveAirlines { get; set; }

        public int TotalGates { get; set; }
        public int ActiveOpenGates { get; set; }

        public int TotalCheckInDesks { get; set; }
        public int ActiveCheckInDesks { get; set; }

        public int TotalFlights { get; set; }
        public int ActiveFlights { get; set; }
        public int FlightsToday { get; set; }

        public List<FlightSummaryItem> LatestFlights { get; set; } = new();
    }

    public class FlightSummaryItem
    {
        public int Id { get; set; }
        public string FlightNumber { get; set; }
        public string AirlineName { get; set; }
        public string DestinationAirport { get; set; }
        public DateTime DepartureTime { get; set; }
        public string Status { get; set; }
    }
}
