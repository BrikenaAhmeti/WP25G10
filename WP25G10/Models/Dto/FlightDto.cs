namespace WP25G10.Models.Dto
{
    public class FlightDto
    {
        public int Id { get; set; }
        public string FlightNumber { get; set; } = string.Empty;
        public string OriginAirport { get; set; } = string.Empty;
        public string DestinationAirport { get; set; } = string.Empty;
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public string Status { get; set; } = string.Empty;

        public string AirlineName { get; set; } = string.Empty;
        public string AirlineCode { get; set; } = string.Empty;

        public string? GateTerminal { get; set; }
        public string? GateCode { get; set; }
    }
}
