using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WP25G10.Data;
using WP25G10.Models;
using WP25G10.Models.ViewModels;

namespace WP25G10.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class FlightsController : Controller
    {
        private const string HOME_AIRPORT_LABEL = "Prishtina";
        private static string Clean(string? s) => (s ?? "").Trim();

        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public FlightsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(
            string? search,
            string? status,
            string? board,
            int? airlineId,
            int? gateId,
            string? terminal,
            FlightStatus? flightStatus,
            DateTime? date,
            bool? delayedOnly,
            bool? reset
        )
        {
            if (reset == true)
            {
                HttpContext.Session.Remove("Flights_Search");
                HttpContext.Session.Remove("Flights_Status");
                HttpContext.Session.Remove("Flights_Board");
                HttpContext.Session.Remove("Flights_Terminal");
                HttpContext.Session.Remove("Flights_AirlineId");
                HttpContext.Session.Remove("Flights_GateId");
                HttpContext.Session.Remove("Flights_FlightStatus");
                HttpContext.Session.Remove("Flights_Date");
                HttpContext.Session.Remove("Flights_DelayedOnly");
                return RedirectToAction(nameof(Index));
            }

            var hasQuery =
                Request.QueryString.HasValue ||
                !string.IsNullOrWhiteSpace(search) ||
                !string.IsNullOrWhiteSpace(status) ||
                !string.IsNullOrWhiteSpace(board) ||
                airlineId.HasValue ||
                gateId.HasValue ||
                !string.IsNullOrWhiteSpace(terminal) ||
                flightStatus.HasValue ||
                date.HasValue ||
                delayedOnly.HasValue;

            if (!hasQuery)
            {
                search ??= HttpContext.Session.GetString("Flights_Search");
                status ??= HttpContext.Session.GetString("Flights_Status");
                board ??= HttpContext.Session.GetString("Flights_Board");
                terminal ??= HttpContext.Session.GetString("Flights_Terminal");

                var airlineIdStored = HttpContext.Session.GetInt32("Flights_AirlineId");
                if (airlineIdStored.HasValue) airlineId ??= airlineIdStored.Value;

                var gateIdStored = HttpContext.Session.GetInt32("Flights_GateId");
                if (gateIdStored.HasValue) gateId ??= gateIdStored.Value;

                var flightStatusStr = HttpContext.Session.GetString("Flights_FlightStatus");
                if (!string.IsNullOrEmpty(flightStatusStr) &&
                    Enum.TryParse<FlightStatus>(flightStatusStr, out var fs))
                {
                    flightStatus ??= fs;
                }

                var dateStr = HttpContext.Session.GetString("Flights_Date");
                if (!string.IsNullOrEmpty(dateStr) && DateTime.TryParse(dateStr, out var dt))
                {
                    date ??= dt;
                }

                var delayedStr = HttpContext.Session.GetString("Flights_DelayedOnly");
                if (!string.IsNullOrEmpty(delayedStr))
                {
                    delayedOnly ??= delayedStr == "true";
                }
            }

            board = string.IsNullOrWhiteSpace(board) ? "departures" : board.Trim().ToLowerInvariant();

            // status "all" now means "all ACTIVE flights" (default).
            status = string.IsNullOrWhiteSpace(status) ? "all" : status.Trim().ToLowerInvariant();
            if (status != "inactive") status = "all";

            HttpContext.Session.SetString("Flights_Search", search ?? string.Empty);
            HttpContext.Session.SetString("Flights_Status", status);
            HttpContext.Session.SetString("Flights_Board", board);
            HttpContext.Session.SetString("Flights_Terminal", terminal ?? string.Empty);

            if (airlineId.HasValue) HttpContext.Session.SetInt32("Flights_AirlineId", airlineId.Value);
            else HttpContext.Session.Remove("Flights_AirlineId");

            if (gateId.HasValue) HttpContext.Session.SetInt32("Flights_GateId", gateId.Value);
            else HttpContext.Session.Remove("Flights_GateId");

            if (flightStatus.HasValue) HttpContext.Session.SetString("Flights_FlightStatus", flightStatus.Value.ToString());
            else HttpContext.Session.Remove("Flights_FlightStatus");

            if (date.HasValue) HttpContext.Session.SetString("Flights_Date", date.Value.ToString("yyyy-MM-dd"));
            else HttpContext.Session.Remove("Flights_Date");

            HttpContext.Session.SetString("Flights_DelayedOnly", (delayedOnly == true) ? "true" : "false");

            var q = _context.Flights
                .Include(f => f.Airline)
                .Include(f => f.Gate)
                .Include(f => f.CheckInDesk)
                .Include(f => f.CreatedByUser)
                .AsQueryable();

            if (status == "inactive") q = q.Where(f => !f.IsActive);
            else q = q.Where(f => f.IsActive);

            if (board == "arrivals")
                q = q.Where(f => f.Type == FlightType.Arrival);
            else if (board == "departures")
                q = q.Where(f => f.Type == FlightType.Departure);
            else
                board = "all";

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

            if (airlineId.HasValue) q = q.Where(f => f.AirlineId == airlineId.Value);
            if (gateId.HasValue) q = q.Where(f => f.GateId == gateId.Value);

            if (!string.IsNullOrWhiteSpace(terminal))
            {
                var t = terminal.Trim();
                q = q.Where(f =>
                    (f.Gate != null && f.Gate.Terminal == t) ||
                    (f.CheckInDesk != null && f.CheckInDesk.Terminal == t));
            }

            if (flightStatus.HasValue) q = q.Where(f => f.Status == flightStatus.Value);

            if (delayedOnly == true)
                q = q.Where(f => f.DelayMinutes > 0 || f.Status == FlightStatus.Delayed);

            if (date.HasValue)
            {
                var start = date.Value.Date;
                var end = start.AddDays(1);

                if (board == "arrivals")
                    q = q.Where(f => f.ArrivalTime >= start && f.ArrivalTime < end);
                else if (board == "departures")
                    q = q.Where(f => f.DepartureTime >= start && f.DepartureTime < end);
                else
                    q = q.Where(f =>
                        (f.DepartureTime >= start && f.DepartureTime < end) ||
                        (f.ArrivalTime >= start && f.ArrivalTime < end));
            }

            q = board == "arrivals"
                ? q.OrderBy(f => f.ArrivalTime)
                : q.OrderBy(f => f.DepartureTime);

            var flights = await q.ToListAsync();

            ViewBag.Airlines = await _context.Airlines
                .Where(a => a.IsActive)
                .OrderBy(a => a.Name)
                .Select(a => new SelectListItem($"{a.Name} ({a.Code})", a.Id.ToString()))
                .ToListAsync();

            ViewBag.Gates = await _context.Gates
                .Where(g => g.IsActive)
                .OrderBy(g => g.Terminal).ThenBy(g => g.Code)
                .Select(g => new SelectListItem($"{g.Terminal} - {g.Code}", g.Id.ToString()))
                .ToListAsync();

            var vm = new FlightsIndexViewModel
            {
                Flights = flights,
                SearchTerm = search,
                StatusFilter = status,
                Board = board,
                AirlineId = airlineId,
                GateId = gateId,
                Terminal = terminal,
                FlightStatus = flightStatus,
                Date = date,
                DelayedOnly = delayedOnly == true
            };

            return View(vm);
        }

        public async Task<IActionResult> Details(int id)
        {
            var flight = await _context.Flights
                .Include(f => f.Airline)
                .Include(f => f.Gate)
                .Include(f => f.CheckInDesk)
                .Include(f => f.CreatedByUser)
                .FirstOrDefaultAsync(f => f.Id == id && f.IsActive);

            if (flight == null) return NotFound();
            return View(flight);
        }

        public async Task<IActionResult> Create()
        {
            var vm = new FlightFormViewModel
            {
                Type = FlightType.Departure,
                DepartureTime = DateTime.Now.AddHours(2),
                ArrivalTime = DateTime.Now.AddHours(4),

                // so UI shows it immediately (even though we enforce server-side)
                OriginAirport = HOME_AIRPORT_LABEL,
                DestinationAirport = "",

                Airlines = await AirlineSelect(),
                Gates = await GateSelect(),
                CheckInDesks = await CheckInDeskSelect(),
            };

            ViewBag.HomeAirport = HOME_AIRPORT_LABEL;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FlightFormViewModel vm)
        {
            if (vm.ArrivalTime <= vm.DepartureTime)
                ModelState.AddModelError("", "Arrival time must be after departure time.");

            var origin = "";
            var dest = "";

            if (vm.Type == FlightType.Departure)
            {
                origin = HOME_AIRPORT_LABEL;
                dest = Clean(vm.DestinationAirport);

                if (string.IsNullOrWhiteSpace(dest))
                    ModelState.AddModelError(nameof(vm.DestinationAirport), "Destination is required for departures.");
            }
            else
            {
                dest = HOME_AIRPORT_LABEL;
                origin = Clean(vm.OriginAirport);

                if (string.IsNullOrWhiteSpace(origin))
                    ModelState.AddModelError(nameof(vm.OriginAirport), "Origin is required for arrivals.");
            }

            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User) ?? "";

                var flight = new Flight
                {
                    FlightNumber = Clean(vm.FlightNumber),
                    Type = vm.Type,
                    AirlineId = vm.AirlineId,
                    GateId = vm.GateId,
                    CheckInDeskId = vm.CheckInDeskId,
                    OriginAirport = origin,
                    DestinationAirport = dest,
                    DepartureTime = vm.DepartureTime,
                    ArrivalTime = vm.ArrivalTime,
                    Status = vm.Status,
                    DelayMinutes = vm.DelayMinutes,
                    CreatedByUserId = userId,
                    IsActive = true
                };

                var overlapError = await ValidateNoOverlap(flight, null);
                if (overlapError != null)
                {
                    ModelState.AddModelError("", overlapError);
                }
                else
                {
                    _context.Flights.Add(flight);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index), new { board = vm.Type == FlightType.Arrival ? "arrivals" : "departures" });
                }
            }

            vm.Airlines = await AirlineSelect();
            vm.Gates = await GateSelect();
            vm.CheckInDesks = await CheckInDeskSelect();
            ViewBag.HomeAirport = HOME_AIRPORT_LABEL;
            return View(vm);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var flight = await _context.Flights.FirstOrDefaultAsync(f => f.Id == id && f.IsActive);
            if (flight == null) return NotFound();

            var vm = new FlightFormViewModel
            {
                Id = flight.Id,
                FlightNumber = flight.FlightNumber,
                Type = flight.Type,
                AirlineId = flight.AirlineId,
                GateId = flight.GateId,
                CheckInDeskId = flight.CheckInDeskId,

                // populate both so UI can show the fixed one too
                OriginAirport = flight.OriginAirport,
                DestinationAirport = flight.DestinationAirport,

                DepartureTime = flight.DepartureTime,
                ArrivalTime = flight.ArrivalTime,
                Status = flight.Status,
                DelayMinutes = flight.DelayMinutes,

                Airlines = await AirlineSelect(),
                Gates = await GateSelect(),
                CheckInDesks = await CheckInDeskSelect()
            };

            ViewBag.HomeAirport = HOME_AIRPORT_LABEL;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FlightFormViewModel vm)
        {
            if (vm.Id == null || id != vm.Id.Value) return BadRequest();

            if (vm.ArrivalTime <= vm.DepartureTime)
                ModelState.AddModelError("", "Arrival time must be after departure time.");

            var flight = await _context.Flights.FirstOrDefaultAsync(f => f.Id == id && f.IsActive);
            if (flight == null) return NotFound();

            var origin = "";
            var dest = "";

            if (vm.Type == FlightType.Departure)
            {
                origin = HOME_AIRPORT_LABEL;
                dest = Clean(vm.DestinationAirport);
                if (string.IsNullOrWhiteSpace(dest))
                    ModelState.AddModelError(nameof(vm.DestinationAirport), "Destination is required for departures.");
            }
            else
            {
                dest = HOME_AIRPORT_LABEL;
                origin = Clean(vm.OriginAirport);
                if (string.IsNullOrWhiteSpace(origin))
                    ModelState.AddModelError(nameof(vm.OriginAirport), "Origin is required for arrivals.");
            }

            if (ModelState.IsValid)
            {
                flight.FlightNumber = Clean(vm.FlightNumber);
                flight.Type = vm.Type;
                flight.AirlineId = vm.AirlineId;
                flight.GateId = vm.GateId;
                flight.CheckInDeskId = vm.CheckInDeskId;
                flight.OriginAirport = origin;
                flight.DestinationAirport = dest;
                flight.DepartureTime = vm.DepartureTime;
                flight.ArrivalTime = vm.ArrivalTime;
                flight.Status = vm.Status;
                flight.DelayMinutes = vm.DelayMinutes;

                var overlapError = await ValidateNoOverlap(flight, id);
                if (overlapError != null)
                {
                    ModelState.AddModelError("", overlapError);
                }
                else
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index), new { board = vm.Type == FlightType.Arrival ? "arrivals" : "departures" });
                }
            }

            vm.Airlines = await AirlineSelect();
            vm.Gates = await GateSelect();
            vm.CheckInDesks = await CheckInDeskSelect();
            ViewBag.HomeAirport = HOME_AIRPORT_LABEL;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(int id)
        {
            var flight = await _context.Flights.FirstOrDefaultAsync(f => f.Id == id && f.IsActive);
            if (flight == null) return NotFound();

            flight.IsActive = false;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private Task<List<SelectListItem>> AirlineSelect() =>
            _context.Airlines.Where(a => a.IsActive)
                .OrderBy(a => a.Name)
                .Select(a => new SelectListItem($"{a.Name} ({a.Code})", a.Id.ToString()))
                .ToListAsync();

        private Task<List<SelectListItem>> GateSelect() =>
            _context.Gates.Where(g => g.IsActive && g.Status == GateStatus.Open)
                .OrderBy(g => g.Terminal).ThenBy(g => g.Code)
                .Select(g => new SelectListItem($"{g.Terminal} - {g.Code}", g.Id.ToString()))
                .ToListAsync();

        private Task<List<SelectListItem>> CheckInDeskSelect() =>
            _context.CheckInDesks.Where(d => d.IsActive)
                .OrderBy(d => d.Terminal).ThenBy(d => d.DeskNumber)
                .Select(d => new SelectListItem($"{d.Terminal} - Desk {d.DeskNumber}", d.Id.ToString()))
                .ToListAsync();

        private async Task<string?> ValidateNoOverlap(Flight candidate, int? editingId)
        {
            var start = candidate.DepartureTime;
            var end = candidate.ArrivalTime;

            var gateOverlap = await _context.Flights
                .Where(f => f.IsActive
                    && f.GateId == candidate.GateId
                    && (!editingId.HasValue || f.Id != editingId.Value)
                    && start < f.ArrivalTime
                    && end > f.DepartureTime)
                .Include(f => f.Gate)
                .FirstOrDefaultAsync();

            if (gateOverlap != null)
                return $"Gate conflict: another active flight already uses gate {gateOverlap.Gate?.Terminal}-{gateOverlap.Gate?.Code} during that time.";

            if (candidate.CheckInDeskId.HasValue)
            {
                var deskOverlap = await _context.Flights
                    .Where(f => f.IsActive
                        && f.CheckInDeskId == candidate.CheckInDeskId
                        && (!editingId.HasValue || f.Id != editingId.Value)
                        && start < f.ArrivalTime
                        && end > f.DepartureTime)
                    .Include(f => f.CheckInDesk)
                    .FirstOrDefaultAsync();

                if (deskOverlap != null)
                    return $"Check-in desk conflict: desk {deskOverlap.CheckInDesk?.Terminal} - Desk {deskOverlap.CheckInDesk?.DeskNumber} is already used during that time.";
            }

            return null;
        }
    }
}