using System;
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
    public class GatesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public GatesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // get index page view - /Admin/Gates
        public async Task<IActionResult> Index(
            string? search,
            string status = "all",
            string gateState = "all",
            string sort = "created_desc",
            int page = 1,
            int pageSize = 10)
        {
            var query = _context.Gates.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(g =>
                    g.Terminal.ToLower().Contains(s) ||
                    g.Code.ToLower().Contains(s));
            }

            switch (status)
            {
                case "active":
                    query = query.Where(g => g.IsActive);
                    break;
                case "inactive":
                    query = query.Where(g => !g.IsActive);
                    break;
            }

            switch (gateState)
            {
                case "open":
                    query = query.Where(g => g.Status == GateStatus.Open);
                    break;
                case "closed":
                    query = query.Where(g => g.Status == GateStatus.Closed);
                    break;
            }

            var totalCount = await query.CountAsync();

            switch (sort)
            {
                case "terminal_asc":
                    query = query.OrderBy(g => g.Terminal)
                                 .ThenBy(g => g.Code);
                    break;

                case "terminal_desc":
                    query = query.OrderByDescending(g => g.Terminal)
                                 .ThenByDescending(g => g.Code);
                    break;

                case "code_asc":
                    query = query.OrderBy(g => g.Code)
                                 .ThenBy(g => g.Terminal);
                    break;

                case "code_desc":
                    query = query.OrderByDescending(g => g.Code)
                                 .ThenByDescending(g => g.Terminal);
                    break;

                default:
                    sort = "created_desc";
                    query = query.OrderByDescending(g => g.Id);
                    break;
            }

            if (page < 1) page = 1;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var gates = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new GatesIndexViewModel
            {
                Gates = gates,
                SearchTerm = search,
                StatusFilter = status,
                GateStatusFilter = gateState,
                SortOrder = sort,
                PageNumber = page,
                TotalPages = totalPages
            };

            return View(vm);
        }

        // get details view - /Admin/Gates/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var gate = await _context.Gates
                .Include(g => g.CreatedByUser)
                .Include(g => g.Flights)
                    .ThenInclude(f => f.Airline)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (gate == null) return NotFound();

            return View(gate);
        }

        // get create view - /Admin/Gates/Create
        public IActionResult Create() => View();

        // post endpoint for create: /Admin/Gates/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Gate gate)
        {
            if (!ModelState.IsValid) return View(gate);

            gate.CreatedByUserId = _userManager.GetUserId(User)!;
            _context.Gates.Add(gate);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // get edit view ednpoint - /Admin/Gates/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var gate = await _context.Gates.FindAsync(id);
            if (gate == null) return NotFound();

            return View(gate);
        }

        // post edit endpiont - /Admin/Gates/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Gate gate)
        {
            if (id != gate.Id) return BadRequest();

            if (!ModelState.IsValid) return View(gate);

            var existing = await _context.Gates
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == id);
            if (existing == null) return NotFound();

            gate.CreatedByUserId = existing.CreatedByUserId;

            _context.Update(gate);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // delete endpoint - /Admin/Gates/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var gate = await _context.Gates.FindAsync(id);
            if (gate == null) return NotFound();

            _context.Gates.Remove(gate);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
