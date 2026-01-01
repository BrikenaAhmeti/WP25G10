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
    [Authorize(Roles = "Admin")]
    public class FlightsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public FlightsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Admin/Flights


public async Task<IActionResult> Index(
    string? search,
    string? status,
    string? board,
    int? airlineId,
    int? gateId,
    string? terminal,
    FlightStatus? flightStatus,
    DateTime? date,
    bool? delayedOnly
)
    {
        board = string.IsNullOrWhiteSpace(board) ? "departures" : board.ToLower();
        status = string.IsNullOrWhiteSpace(status) ? "all" : status.ToLower();

        var q = _context.Flights
            .Include(f => f.Airline)
            .Include(f => f.Gate)
            .Include(f => f.CheckInDesk)
            .Include(f => f.CreatedByUser)
            .AsQueryable();

        // active/inactive filter (same style as Airlines)
        if (status == "active") q = q.Where(f => f.IsActive);
        else if (status == "inactive") q = q.Where(f => !f.IsActive);

        // search filter (flight no / origin / destination)
        if (!string.IsNullOrWhiteSpace(search))
        {
            q = q.Where(f =>
                f.FlightNumber.Contains(search) ||
                f.OriginAirport.Contains(search) ||
                f.DestinationAirport.Contains(search));
        }

        if (airlineId.HasValue) q = q.Where(f => f.AirlineId == airlineId.Value);
        if (gateId.HasValue) q = q.Where(f => f.GateId == gateId.Value);

        if (!string.IsNullOrWhiteSpace(terminal))
        {
            q = q.Where(f =>
                (f.Gate != null && f.Gate.Terminal == terminal) ||
                (f.CheckInDesk != null && f.CheckInDesk.Terminal == terminal));
        }

        if (flightStatus.HasValue) q = q.Where(f => f.Status == flightStatus.Value);

        if (delayedOnly == true)
            q = q.Where(f => f.DelayMinutes > 0 || f.Status == FlightStatus.Delayed);

        if (date.HasValue)
        {
            var d = date.Value.Date;
            q = board == "arrivals"
                ? q.Where(f => f.ArrivalTime.Date == d)
                : q.Where(f => f.DepartureTime.Date == d);
        }

        q = board == "arrivals"
            ? q.OrderBy(f => f.ArrivalTime)
            : q.OrderBy(f => f.DepartureTime);

        var flights = await q.ToListAsync();

        // dropdowns for filters (ViewBag like we used in the view)
        ViewBag.Airlines = await _context.Airlines.Where(a => a.IsActive)
            .OrderBy(a => a.Name)
            .Select(a => new SelectListItem($"{a.Name} ({a.Code})", a.Id.ToString()))
            .ToListAsync();

        ViewBag.Gates = await _context.Gates.Where(g => g.IsActive)
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


    // GET: /Admin/Flights/Details/5
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

        // GET: /Admin/Flights/Create
        public async Task<IActionResult> Create()
        {
            var vm = new FlightFormViewModel
            {
                DepartureTime = DateTime.Now.AddHours(2),
                ArrivalTime = DateTime.Now.AddHours(4),
                Airlines = await AirlineSelect(),
                Gates = await GateSelect(),
                CheckInDesks = await CheckInDeskSelect()
            };
            return View(vm);
        }

        // POST: /Admin/Flights/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FlightFormViewModel vm)
        {
            if (vm.ArrivalTime <= vm.DepartureTime)
                ModelState.AddModelError("", "Arrival time must be after departure time.");

            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User) ?? "";

                var flight = new Flight
                {
                    FlightNumber = vm.FlightNumber.Trim(),
                    AirlineId = vm.AirlineId,
                    GateId = vm.GateId,
                    CheckInDeskId = vm.CheckInDeskId,
                    OriginAirport = vm.OriginAirport.Trim(),
                    DestinationAirport = vm.DestinationAirport.Trim(),
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
                    return RedirectToAction(nameof(Index));
                }
            }

            vm.Airlines = await AirlineSelect();
            vm.Gates = await GateSelect();
            vm.CheckInDesks = await CheckInDeskSelect();
            return View(vm);
        }

        // GET: /Admin/Flights/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var flight = await _context.Flights.FirstOrDefaultAsync(f => f.Id == id && f.IsActive);
            if (flight == null) return NotFound();

            var vm = new FlightFormViewModel
            {
                Id = flight.Id,
                FlightNumber = flight.FlightNumber,
                AirlineId = flight.AirlineId,
                GateId = flight.GateId,
                CheckInDeskId = flight.CheckInDeskId,
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

            return View(vm);
        }

        // POST: /Admin/Flights/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FlightFormViewModel vm)
        {
            if (id != vm.Id) return BadRequest();

            if (vm.ArrivalTime <= vm.DepartureTime)
                ModelState.AddModelError("", "Arrival time must be after departure time.");

            var flight = await _context.Flights.FirstOrDefaultAsync(f => f.Id == id && f.IsActive);
            if (flight == null) return NotFound();

            if (ModelState.IsValid)
            {
                flight.FlightNumber = vm.FlightNumber.Trim();
                flight.AirlineId = vm.AirlineId;
                flight.GateId = vm.GateId;
                flight.CheckInDeskId = vm.CheckInDeskId;
                flight.OriginAirport = vm.OriginAirport.Trim();
                flight.DestinationAirport = vm.DestinationAirport.Trim();
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
                    return RedirectToAction(nameof(Index));
                }
            }

            vm.Airlines = await AirlineSelect();
            vm.Gates = await GateSelect();
            vm.CheckInDesks = await CheckInDeskSelect();
            return View(vm);
        }

        // GET: /Admin/Flights/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var flight = await _context.Flights
                .Include(f => f.Airline)
                .Include(f => f.Gate)
                .Include(f => f.CheckInDesk)
                .FirstOrDefaultAsync(f => f.Id == id && f.IsActive);

            if (flight == null) return NotFound();
            return View(flight);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
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
                return $"Gate conflict: another flight already uses gate {gateOverlap.Gate?.Terminal}-{gateOverlap.Gate?.Code} during that time.";

            // Check-in desk overlap (if selected)
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
                    return $"Check-in desk conflict: desk {deskOverlap.CheckInDesk?.Terminal} - {deskOverlap.CheckInDesk?.DeskNumber} is already used during that time.";
            }

            return null;
        }
    }
}
