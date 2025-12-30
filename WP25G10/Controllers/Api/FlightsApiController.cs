using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WP25G10.Data;

namespace WP25G10.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class FlightsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FlightsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var flights = await _context.Flights
                .Include(f => f.Airline)
                .Include(f => f.Gate)
                .Where(f => f.IsActive)
                .ToListAsync();
            return Ok(flights);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var flight = await _context.Flights
                .Include(f => f.Airline)
                .Include(f => f.Gate)
                .FirstOrDefaultAsync(f => f.Id == id && f.IsActive);

            if (flight == null) return NotFound();
            return Ok(flight);
        }
    }
}
