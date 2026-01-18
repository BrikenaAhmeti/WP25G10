using System;

namespace WP25G10.Models.Dto
{
    public class FlightOpsStatsDto
    {
        public DateTime Date { get; set; }
        public int ArrivalsToday { get; set; }
        public int DeparturesToday { get; set; }
        public int DelayedToday { get; set; }
        public int Next60Count { get; set; }
        public int ActiveGates { get; set; }
    }
}