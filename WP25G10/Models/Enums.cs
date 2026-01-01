namespace WP25G10.Models
{
    public enum GateStatus
    {
        Open,
        Closed,
        Maintenance
    }

    public enum FlightStatus
    {
        Scheduled = 0,
        Boarding = 1,
        Departed = 2,
        Delayed = 3,
        Cancelled = 4,
        Arrived = 5
    }
}
