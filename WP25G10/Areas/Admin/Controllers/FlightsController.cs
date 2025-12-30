using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WP25G10.Data;
using WP25G10.Models;

namespace WP25G10.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class FlightsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public FlightsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(
            string? airline,
            string? origin,
            string? destination,
            FlightStatus? status,
            DateTime? date,
            int page = 1,
            int pageSize = 10)
        {
            var query = _context.Flights
                .Include(f => f.Airline)
                .Include(f => f.Gate)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(airline))
            {
                query = query.Where(f =>
                    f.Airline != null &&
                    (f.Airline.Name.Contains(airline) || f.Airline.Code.Contains(airline)));
            }

            if (!string.IsNullOrWhiteSpace(origin))
            {
                query = query.Where(f => f.OriginAirport.Contains(origin));
            }

            if (!string.IsNullOrWhiteSpace(destination))
            {
                query = query.Where(f => f.DestinationAirport.Contains(destination));
            }

            if (status.HasValue)
            {
                query = query.Where(f => f.Status == status.Value);
            }

            if (date.HasValue)
            {
                var d = date.Value.Date;
                query = query.Where(f => f.DepartureTime.Date == d);
            }

            var totalItems = await query.CountAsync();
            var flights = await query
                .OrderBy(f => f.DepartureTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.AirlineFilter = airline;
            ViewBag.OriginFilter = origin;
            ViewBag.DestinationFilter = destination;
            ViewBag.StatusFilter = status;
            ViewBag.DateFilter = date?.ToString("yyyy-MM-dd");

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;

            return View(flights);
        }

        public async Task<IActionResult> Details(int id)
        {
            var flight = await _context.Flights
                .Include(f => f.Airline)
                .Include(f => f.Gate)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (flight == null)
            {
                return NotFound();
            }

            return View(flight);
        }

        private void PopulateDropdowns(int? airlineId = null, int? gateId = null)
        {
            ViewBag.AirlineId = new SelectList(
                _context.Airlines
                    .Where(a => a.IsActive)
                    .OrderBy(a => a.Name),
                "Id", "Name", airlineId);

            ViewBag.GateId = new SelectList(
                _context.Gates
                    .Where(g => g.IsActive && g.Status == GateStatus.Open)
                    .OrderBy(g => g.Terminal).ThenBy(g => g.Code),
                "Id", "Code", gateId);
        }

        // GET: Admin/Flights/Create
        public IActionResult Create()
        {
            PopulateDropdowns();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Flight flight)
        {
            if (!ModelState.IsValid)
            {
                PopulateDropdowns(flight.AirlineId, flight.GateId);
                return View(flight);
            }

            flight.CreatedByUserId = _userManager.GetUserId(User)!;
            _context.Flights.Add(flight);
            await _context.SaveChangesAsync();

            await LogAsync("Create", "Flight", flight.Id,
                $"Created flight {flight.FlightNumber} from {flight.OriginAirport} to {flight.DestinationAirport}");

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var flight = await _context.Flights.FindAsync(id);
            if (flight == null)
            {
                return NotFound();
            }

            PopulateDropdowns(flight.AirlineId, flight.GateId);
            return View(flight);
        }

        // POST: Admin/Flights/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Flight flight)
        {
            if (id != flight.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                PopulateDropdowns(flight.AirlineId, flight.GateId);
                return View(flight);
            }

            try
            {
                _context.Entry(flight).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                await LogAsync("Edit", "Flight", flight.Id,
                    $"Edited flight {flight.FlightNumber}");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await FlightExists(flight.Id))
                {
                    return NotFound();
                }
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var flight = await _context.Flights
                .Include(f => f.Airline)
                .Include(f => f.Gate)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (flight == null)
            {
                return NotFound();
            }

            return View(flight);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var flight = await _context.Flights.FindAsync(id);
            if (flight != null)
            {
                _context.Flights.Remove(flight);
                await _context.SaveChangesAsync();

                await LogAsync("Delete", "Flight", id,
                    $"Deleted flight {flight.FlightNumber}");
            }

            return RedirectToAction(nameof(Index));
        }

        private Task<bool> FlightExists(int id)
        {
            return _context.Flights.AnyAsync(f => f.Id == id);
        }

        private async Task LogAsync(string action, string entityName, int entityId, string details)
        {
            var log = new ActionLog
            {
                UserId = _userManager.GetUserId(User),
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Details = details,
                Timestamp = DateTime.UtcNow
            };

            _context.ActionLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
