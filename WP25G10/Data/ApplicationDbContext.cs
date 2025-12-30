using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WP25G10.Models;

namespace WP25G10.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Airline> Airlines { get; set; } = default!;
    public DbSet<Gate> Gates { get; set; } = default!;
    public DbSet<CheckInDesk> CheckInDesks { get; set; } = default!;
    public DbSet<Flight> Flights { get; set; } = default!;
    public DbSet<ActionLog> ActionLogs { get; set; } = default!;
}
