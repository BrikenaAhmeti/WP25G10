using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WP25G10.Data;
using WP25G10.Models;
using WP25G10.Models.ViewModels;


namespace WP25G10.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AirlinesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AirlinesController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(
            string? search,
            string status = "all",
            int page = 1,
            int pageSize = 10)
        {
            var query = _context.Airlines.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();

                query = query.Where(a =>
                    a.Name.ToLower().Contains(s) ||
                    a.Code.ToLower().Contains(s) ||
                    (a.Country != null && a.Country.ToLower().Contains(s)));
            }

            switch (status)
            {
                case "active":
                    query = query.Where(a => a.IsActive);
                    break;
                case "inactive":
                    query = query.Where(a => !a.IsActive);
                    break;
            }

            var totalCount = await query.CountAsync();

            if (page < 1) page = 1;
            var totalPages = (int)System.Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var airlines = await query
                .OrderBy(a => a.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new AirlinesIndexViewModel
            {
                Airlines = airlines,
                SearchTerm = search,
                StatusFilter = status,
                PageNumber = page,
                TotalPages = totalPages
            };

            return View(vm);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Airline airline)
        {
            airline.CreatedByUserId = _userManager.GetUserId(User);

            if (!ModelState.IsValid)
            {
                ViewBag.Errors = string.Join(" | ",
                    ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));

                return View(airline);
            }

            _context.Add(airline);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var airline = await _context.Airlines.FindAsync(id);
            if (airline == null) return NotFound();

            return View(airline);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
        int id,
        [Bind("Id,Name,Code,Country,LogoUrl,IsActive")] Airline airline)
        {
            if (id != airline.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                return View(airline);
            }

            var existing = await _context.Airlines
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);

            if (existing == null) return NotFound();

            airline.CreatedByUserId = existing.CreatedByUserId;

            _context.Update(airline);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var airline = await _context.Airlines
                .FirstOrDefaultAsync(m => m.Id == id);

            if (airline == null) return NotFound();

            return View(airline);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var airline = await _context.Airlines.FindAsync(id);
            if (airline != null)
            {
                _context.Airlines.Remove(airline);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool AirlineExists(int id)
        {
            return _context.Airlines.Any(e => e.Id == id);
        }
    }
}
