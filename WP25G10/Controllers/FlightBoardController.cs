using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WP25G10.Data;
using Microsoft.AspNetCore.Http;
using WP25G10.Models;

namespace WP25G10.Controllers
{
    [AllowAnonymous] // public – no login required
    public class FlightBoardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FlightBoardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // viewType: "departures" or "arrivals" (you can use it in the View as needed)
        public async Task<IActionResult> Index(
            string? viewType,
            string? airline,
            string? origin,
            string? destination,
            FlightStatus? status,
            DateTime? date)
        {
            // base query: only active flights
            var query = _context.Flights
                .Include(f => f.Airline)
                .Include(f => f.Gate)
                .Where(f => f.IsActive)
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

            // You can later use viewType to show departures/arrivals differently in the view
            ViewBag.ViewType = string.IsNullOrEmpty(viewType) ? "departures" : viewType.ToLower();

            // Simple ordering by departure time
            var flights = await query
                .OrderBy(f => f.DepartureTime)
                .ToListAsync();

            // decide view type first as a string
            var viewTypeValue = string.IsNullOrEmpty(viewType)
                ? "departures"
                : viewType.ToLower();

            ViewBag.ViewType = viewTypeValue;

            // Optional: remember filters in Session (Requirement 12)
            HttpContext.Session.SetString("LastAirline", airline ?? string.Empty);
            HttpContext.Session.SetString("LastOrigin", origin ?? string.Empty);
            HttpContext.Session.SetString("LastDestination", destination ?? string.Empty);
            HttpContext.Session.SetString("LastStatus", status?.ToString() ?? string.Empty);
            HttpContext.Session.SetString("LastDate", date?.ToString("yyyy-MM-dd") ?? string.Empty);
            HttpContext.Session.SetString("LastViewType", viewTypeValue);   // 👈 now a string, not dynamic


            return View(flights);
        }
    }
}