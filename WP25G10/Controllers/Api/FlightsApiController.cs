using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
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

        private const string HOME_CITY = "PRISHTINA";
        private const string HOME_CODE = "PRN";

        public FlightsApiController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private static bool IsStaffOrAdmin(ClaimsPrincipal user)
        {
            return user?.Identity?.IsAuthenticated == true &&
                   (user.IsInRole("Admin") || user.IsInRole("Staff"));
        }

        private static FlightStatus? TryParseFlightStatus(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (Enum.TryParse<FlightStatus>(s.Trim(), ignoreCase: true, out var fs)) return fs;
            return null;
        }

        private static string NormalizeBoard(string? board)
        {
            var b = string.IsNullOrWhiteSpace(board) ? "departures" : board.Trim().ToLowerInvariant();
            return (b == "arrivals" || b == "departures" || b == "all") ? b : "departures";
        }

        private static string NormalizeStatus(string? status)
        {
            var s = string.IsNullOrWhiteSpace(status) ? "active" : status.Trim().ToLowerInvariant();
            return (s == "active" || s == "inactive") ? s : "active";
        }

        // GET: /api/flightsApi?board=departures|arrivals|all&search=aaa&date=2026-01-17
        //      &delayedOnly=true&terminal=A&flightStatus=Delayed&airlineId=1&gateId=2&status=active|inactive
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<FlightDto>>> GetFlights(
            [FromQuery] string? board = "departures",
            [FromQuery] string? search = null,
            [FromQuery] DateTime? date = null,
            [FromQuery] bool? delayedOnly = null,
            [FromQuery] string? terminal = null,
            [FromQuery] string? flightStatus = null,
            [FromQuery] int? airlineId = null,
            [FromQuery] int? gateId = null,
            [FromQuery] string? status = "active"
        )
        {
            var b = NormalizeBoard(board);
            var st = NormalizeStatus(status);

            var homeCity = HOME_CITY;
            var homeCode = HOME_CODE;

            var q = _context.Flights
                .AsNoTracking()
                .Include(f => f.Airline)
                .Include(f => f.Gate)
                .Include(f => f.CheckInDesk)
                .AsQueryable();

            if (st == "inactive" && IsStaffOrAdmin(User))
                q = q.Where(f => !f.IsActive);
            else
                q = q.Where(f => f.IsActive);

            if (b == "arrivals")
            {
                q = q.Where(f =>
                    (f.DestinationAirport ?? "").Trim().ToUpper() == homeCode ||
                    (f.DestinationAirport ?? "").Trim().ToUpper() == homeCity
                );
            }
            else if (b == "departures")
            {
                q = q.Where(f =>
                    (f.OriginAirport ?? "").Trim().ToUpper() == homeCode ||
                    (f.OriginAirport ?? "").Trim().ToUpper() == homeCity
                );
            }
            else
            {
                q = q.Where(f =>
                    (f.OriginAirport ?? "").Trim().ToUpper() == homeCode ||
                    (f.OriginAirport ?? "").Trim().ToUpper() == homeCity ||
                    (f.DestinationAirport ?? "").Trim().ToUpper() == homeCode ||
                    (f.DestinationAirport ?? "").Trim().ToUpper() == homeCity
                );
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLowerInvariant();
                q = q.Where(f =>
                    (f.FlightNumber ?? "").ToLower().Contains(s) ||
                    (f.OriginAirport ?? "").ToLower().Contains(s) ||
                    (f.DestinationAirport ?? "").ToLower().Contains(s) ||
                    (f.Airline != null && (
                        (f.Airline.Name ?? "").ToLower().Contains(s) ||
                        (f.Airline.Code ?? "").ToLower().Contains(s)
                    ))
                );
            }

            if (date.HasValue)
            {
                var d = date.Value.Date;

                if (b == "arrivals")
                    q = q.Where(f => f.ArrivalTime.Date == d);
                else if (b == "departures")
                    q = q.Where(f => f.DepartureTime.Date == d);
                else
                    q = q.Where(f => f.DepartureTime.Date == d || f.ArrivalTime.Date == d);
            }

            if (delayedOnly == true)
            {
                q = q.Where(f => f.DelayMinutes > 0 || f.Status == FlightStatus.Delayed);
            }

            var fsParsed = TryParseFlightStatus(flightStatus);
            if (fsParsed.HasValue)
            {
                q = q.Where(f => f.Status == fsParsed.Value);
            }

            if (airlineId.HasValue) q = q.Where(f => f.AirlineId == airlineId.Value);
            if (gateId.HasValue) q = q.Where(f => f.GateId == gateId.Value);

            if (!string.IsNullOrWhiteSpace(terminal))
            {
                var t = terminal.Trim();
                q = q.Where(f =>
                    (f.Gate != null && f.Gate.Terminal == t) ||
                    (f.CheckInDesk != null && f.CheckInDesk.Terminal == t)
                );
            }

            q = (b == "arrivals")
                ? q.OrderBy(f => f.ArrivalTime)
                : q.OrderBy(f => f.DepartureTime);

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

        // GET: /api/flightsApi/stats?date=2026-01-17
        [HttpGet("stats")]
        [AllowAnonymous]
        public async Task<ActionResult<FlightOpsStatsDto>> GetStats([FromQuery] DateTime? date = null)
        {
            var d = (date?.Date) ?? DateTime.Now.Date;
            var start = d;
            var end = d.AddDays(1);

            var homeCity = HOME_CITY;
            var homeCode = HOME_CODE;

            var flightsQ = _context.Flights.AsNoTracking().Where(f => f.IsActive);

            var arrivalsToday = await flightsQ.CountAsync(f =>
                (
                    (f.DestinationAirport ?? "").Trim().ToUpper() == homeCode ||
                    (f.DestinationAirport ?? "").Trim().ToUpper() == homeCity
                ) &&
                f.ArrivalTime >= start && f.ArrivalTime < end
            );

            var departuresToday = await flightsQ.CountAsync(f =>
                (
                    (f.OriginAirport ?? "").Trim().ToUpper() == homeCode ||
                    (f.OriginAirport ?? "").Trim().ToUpper() == homeCity
                ) &&
                f.DepartureTime >= start && f.DepartureTime < end
            );

            var delayedToday = await flightsQ.CountAsync(f =>
                (
                    (f.OriginAirport ?? "").Trim().ToUpper() == homeCode ||
                    (f.OriginAirport ?? "").Trim().ToUpper() == homeCity ||
                    (f.DestinationAirport ?? "").Trim().ToUpper() == homeCode ||
                    (f.DestinationAirport ?? "").Trim().ToUpper() == homeCity
                ) &&
                (
                    (f.DepartureTime >= start && f.DepartureTime < end) ||
                    (f.ArrivalTime >= start && f.ArrivalTime < end)
                ) &&
                (f.DelayMinutes > 0 || f.Status == FlightStatus.Delayed)
            );

            var now = DateTime.Now;
            var next60 = now.AddMinutes(60);

            var next60Count = await flightsQ.CountAsync(f =>
                (
                    (
                        (
                            (f.OriginAirport ?? "").Trim().ToUpper() == homeCode ||
                            (f.OriginAirport ?? "").Trim().ToUpper() == homeCity
                        ) &&
                        f.DepartureTime >= now && f.DepartureTime <= next60
                    )
                    ||
                    (
                        (
                            (f.DestinationAirport ?? "").Trim().ToUpper() == homeCode ||
                            (f.DestinationAirport ?? "").Trim().ToUpper() == homeCity
                        ) &&
                        f.ArrivalTime >= now && f.ArrivalTime <= next60
                    )
                )
            );

            var activeGates = await _context.Gates.AsNoTracking()
                .CountAsync(g => g.IsActive && g.Status == GateStatus.Open);

            return Ok(new FlightOpsStatsDto
            {
                Date = d,
                ArrivalsToday = arrivalsToday,
                DeparturesToday = departuresToday,
                DelayedToday = delayedToday,
                Next60Count = next60Count,
                ActiveGates = activeGates
            });
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

            // ✅ Correct EF Core query: start from Flights and filter by favorites
            var flights = await _context.Flights
                .AsNoTracking()
                .Where(f => f.IsActive)
                .Where(f => _context.FlightFavorites.Any(ff => ff.UserId == userId && ff.FlightId == f.Id))
                .Include(f => f.Airline)
                .Include(f => f.Gate)
                .OrderBy(f => f.DepartureTime)
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
    }
}