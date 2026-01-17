using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using WP25G10.Data;
using WP25G10.Models;
using WP25G10.Models.ViewModels;

namespace WP25G10.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Now.Date;

            var vm = new AdminDashboardViewModel
            {
                TotalAirlines = await _context.Airlines.CountAsync(),
                ActiveAirlines = await _context.Airlines.CountAsync(a => a.IsActive),

                TotalGates = await _context.Gates.CountAsync(),
                ActiveOpenGates = await _context.Gates.CountAsync(g => g.IsActive && g.Status == GateStatus.Open),

                TotalCheckInDesks = await _context.CheckInDesks.CountAsync(),
                ActiveCheckInDesks = await _context.CheckInDesks.CountAsync(d => d.IsActive),

                TotalFlights = await _context.Flights.CountAsync(f => f.IsActive),
                ActiveFlights = await _context.Flights.CountAsync(f => f.IsActive),

                DeparturesToday = await _context.Flights.CountAsync(f =>
                    f.IsActive &&
                    f.Type == FlightType.Departure &&
                    f.DepartureTime.Date == today),

                ArrivalsToday = await _context.Flights.CountAsync(f =>
                    f.IsActive &&
                    f.Type == FlightType.Arrival &&
                    f.ArrivalTime.Date == today),

                FlightsToday = await _context.Flights.CountAsync(f =>
                    f.IsActive && (f.DepartureTime.Date == today || f.ArrivalTime.Date == today))
            };

            vm.LatestFlights = await _context.Flights
                .Where(f => f.IsActive)
                .Include(f => f.Airline)
                .OrderBy(f => f.DepartureTime)
                .Take(5)
                .Select(f => new FlightSummaryItem
                {
                    Id = f.Id,
                    FlightNumber = f.FlightNumber,
                    AirlineName = f.Airline.Name,
                    DestinationAirport = f.DestinationAirport,
                    DepartureTime = f.DepartureTime,
                    Status = f.Status.ToString()
                })
                .ToListAsync();

            return View(vm);
        }
    }
}