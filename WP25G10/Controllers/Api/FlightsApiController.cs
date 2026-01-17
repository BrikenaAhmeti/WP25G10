using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WP25G10.Data;
using WP25G10.Models;
using WP25G10.Models.Dto;

namespace WP25G10.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class FlightsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        private const string HOME_AIRPORT = "PRN";

        public FlightsApiController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /api/flights?board=departures|arrivals|all&search=aaa&date=2026-01-17
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<FlightDto>>> GetFlights(
            [FromQuery] string? board = "departures",
            [FromQuery] string? search = null,
            [FromQuery] DateTime? date = null)
        {
            board = string.IsNullOrWhiteSpace(board) ? "departures" : board.Trim().ToLowerInvariant();

            // Normalize airport codes to uppercase for consistent compare
            var home = HOME_AIRPORT.ToUpperInvariant();

            var q = _context.Flights
                .AsNoTracking()
                .Include(f => f.Airline)
                .Include(f => f.Gate)
                .Where(f => f.IsActive)
                .AsQueryable();

            // ✅ Filter by board properly
            if (board == "arrivals")
            {
                q = q.Where(f => (f.DestinationAirport ?? "").ToUpper() == home);
            }
            else if (board == "departures")
            {
                q = q.Where(f => (f.OriginAirport ?? "").ToUpper() == home);
            }
            else if (board == "all")
            {
                // Show flights that touch the home airport
                q = q.Where(f =>
                    ((f.OriginAirport ?? "").ToUpper() == home) ||
                    ((f.DestinationAirport ?? "").ToUpper() == home));
            }
            else
            {
                q = q.Where(f => (f.OriginAirport ?? "").ToUpper() == home);
                board = "departures";
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLowerInvariant();
                q = q.Where(f =>
                    (f.FlightNumber ?? "").ToLower().Contains(s) ||
                    (f.OriginAirport ?? "").ToLower().Contains(s) ||
                    (f.DestinationAirport ?? "").ToLower().Contains(s));
            }

            if (date.HasValue)
            {
                var d = date.Value.Date;

                if (board == "arrivals")
                {
                    q = q.Where(f => f.ArrivalTime.Date == d);
                }
                else if (board == "departures")
                {
                    q = q.Where(f => f.DepartureTime.Date == d);
                }
                else
                {
                    q = q.Where(f => f.DepartureTime.Date == d || f.ArrivalTime.Date == d);
                }
            }

            if (board == "arrivals")
                q = q.OrderBy(f => f.ArrivalTime);
            else
                q = q.OrderBy(f => f.DepartureTime);

            var flights = await q
                .Select(f => new FlightDto
                {
                    Id = f.Id,
                    FlightNumber = f.FlightNumber,
                    OriginAirport = f.OriginAirport,
                    DestinationAirport = f.DestinationAirport,
                    DepartureTime = f.DepartureTime,
                    ArrivalTime = f.ArrivalTime,
                    Status = f.Status.ToString(),
                    AirlineName = f.Airline != null ? f.Airline.Name : "",
                    AirlineCode = f.Airline != null ? f.Airline.Code : "",
                    GateTerminal = f.Gate != null ? f.Gate.Terminal : null,
                    GateCode = f.Gate != null ? f.Gate.Code : null
                })
                .ToListAsync();

            return Ok(flights);
        }

        [HttpPost("{id:int}/favorite")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "User")]
        public async Task<IActionResult> AddFavorite(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var exists = await _context.Flights.AnyAsync(f => f.Id == id && f.IsActive);
            if (!exists) return NotFound();

            var already = await _context.FlightFavorites
                .FirstOrDefaultAsync(f => f.FlightId == id && f.UserId == userId);

            if (already != null) return NoContent();

            _context.FlightFavorites.Add(new FlightFavorite
            {
                FlightId = id,
                UserId = userId
            });

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("{id:int}/favorite")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "User")]
        public async Task<IActionResult> RemoveFavorite(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var fav = await _context.FlightFavorites
                .FirstOrDefaultAsync(f => f.FlightId == id && f.UserId == userId);

            if (fav == null) return NotFound();

            _context.FlightFavorites.Remove(fav);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("favorites")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "User")]
        public async Task<ActionResult<IEnumerable<FlightDto>>> GetFavorites()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var flights = await _context.FlightFavorites
                .AsNoTracking()
                .Where(f => f.UserId == userId)
                .Select(f => f.Flight)
                .Include(f => f!.Airline)
                .Include(f => f!.Gate)
                .Where(f => f != null && f.IsActive)
                .Select(f => new FlightDto
                {
                    Id = f!.Id,
                    FlightNumber = f.FlightNumber,
                    OriginAirport = f.OriginAirport,
                    DestinationAirport = f.DestinationAirport,
                    DepartureTime = f.DepartureTime,
                    ArrivalTime = f.ArrivalTime,
                    Status = f.Status.ToString(),
                    AirlineName = f.Airline != null ? f.Airline.Name : "",
                    AirlineCode = f.Airline != null ? f.Airline.Code : "",
                    GateTerminal = f.Gate != null ? f.Gate.Terminal : null,
                    GateCode = f.Gate != null ? f.Gate.Code : null
                })
                .ToListAsync();

            return Ok(flights);
        }
    }
}