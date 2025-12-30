using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WP25G10.Data;

namespace WP25G10.Controllers
{
    [AllowAnonymous]
    public class PublicAirlinesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PublicAirlinesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Airlines.Where(a => a.IsActive).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(a => a.Name.Contains(search) || a.Code.Contains(search));
            var list = await query.OrderBy(a => a.Name).ToListAsync();
            return View(list);
        }

        public async Task<IActionResult> Details(int id)
        {
            var airline = await _context.Airlines
                .Include(a => a.Flights.Where(f => f.IsActive))
                .FirstOrDefaultAsync(a => a.Id == id && a.IsActive);

            if (airline == null) return NotFound();
            return View(airline);
        }
    }
}
