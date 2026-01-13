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
    public class CheckInDesksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CheckInDesksController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // get main view endpoint - Admin/CheckInDesks
        public async Task<IActionResult> Index(
            string? search,
            string status = "all",
            string sort = "created_desc",
            int page = 1,
            int pageSize = 10,
            bool reset = false)
        {
            if (reset)
            {
                HttpContext.Session.Remove("Desks_Search");
                HttpContext.Session.Remove("Desks_Status");
                HttpContext.Session.Remove("Desks_Sort");
                HttpContext.Session.Remove("Desks_Page");

                search = null;
                status = "all";
                sort = "created_desc";
                page = 1;
            }
            else
            {
                var hasQuery =
                    Request.Query.ContainsKey("search") ||
                    Request.Query.ContainsKey("status") ||
                    Request.Query.ContainsKey("sort") ||
                    Request.Query.ContainsKey("page");

                if (!hasQuery)
                {
                    search ??= HttpContext.Session.GetString("Desks_Search");
                    status = HttpContext.Session.GetString("Desks_Status") ?? status;
                    sort = HttpContext.Session.GetString("Desks_Sort") ?? sort;

                    var storedPage = HttpContext.Session.GetInt32("Desks_Page");
                    if (storedPage.HasValue && storedPage.Value > 0)
                    {
                        page = storedPage.Value;
                    }
                }
            }
            HttpContext.Session.SetString("Desks_Search", search ?? string.Empty);
            HttpContext.Session.SetString("Desks_Status", status ?? "all");
            HttpContext.Session.SetString("Desks_Sort", sort ?? "created_desc");
            HttpContext.Session.SetInt32("Desks_Page", page);

            var query = _context.CheckInDesks.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(d =>
                    d.Terminal.ToLower().Contains(s) ||
                    d.DeskNumber.ToString().Contains(search.Trim()));
            }

            switch (status)
            {
                case "active":
                    query = query.Where(d => d.IsActive);
                    break;
                case "inactive":
                    query = query.Where(d => !d.IsActive);
                    break;
            }

            var totalCount = await query.CountAsync();

            switch (sort)
            {
                case "terminal_asc":
                    query = query.OrderBy(d => d.Terminal)
                                 .ThenBy(d => d.DeskNumber);
                    break;

                case "terminal_desc":
                    query = query.OrderByDescending(d => d.Terminal)
                                 .ThenByDescending(d => d.DeskNumber);
                    break;

                case "desk_asc":
                    query = query.OrderBy(d => d.DeskNumber)
                                 .ThenBy(d => d.Terminal);
                    break;

                case "desk_desc":
                    query = query.OrderByDescending(d => d.DeskNumber)
                                 .ThenByDescending(d => d.Terminal);
                    break;

                default:
                    sort = "created_desc";
                    query = query.OrderByDescending(d => d.Id);
                    break;
            }

            if (page < 1) page = 1;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var desks = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new CheckInDesksIndexViewModel
            {
                Desks = desks,
                SearchTerm = search,
                StatusFilter = status,
                SortOrder = sort,
                PageNumber = page,
                TotalPages = totalPages
            };

            return View(vm);
        }

        // get endpoint details - Admin/CheckInDesks/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var desk = await _context.CheckInDesks
                .Include(d => d.CreatedByUser)
                .Include(d => d.Flights)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (desk == null)
            {
                return NotFound();
            }

            return View(desk);
        }

        // create view
        public IActionResult Create()
        {
            return View();
        }

        // post create form
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CheckInDesk desk)
        {
            // terminal format
            desk.Terminal = (desk.Terminal ?? string.Empty).Trim().ToUpperInvariant();

            // CreatedByUserId para validimit (se është [Required] dhe s’vjen nga forma)
            desk.CreatedByUserId = _userManager.GetUserId(User)!;

            // largo nga ModelState sepse e plotësojmë në server
            ModelState.Remove(nameof(CheckInDesk.CreatedByUserId));
            ModelState.Remove(nameof(CheckInDesk.CreatedByUser));

            // Terminal dhe deskNumber duhet te jene unike
            var exists = await _context.CheckInDesks
                .AnyAsync(d => d.Terminal == desk.Terminal &&
                               d.DeskNumber == desk.DeskNumber);

            if (exists)
            {
                ModelState.AddModelError(string.Empty,
                    "A desk with this terminal and number already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(desk);
            }

            _context.CheckInDesks.Add(desk);
            await _context.SaveChangesAsync();

            await LogAsync("Create", "CheckInDesk", desk.Id,
                $"Created desk {desk.Terminal} / {desk.DeskNumber}");

            return RedirectToAction(nameof(Index));
        }

        // get edit view - Admin/CheckInDesks/Edit/2
        public async Task<IActionResult> Edit(int id)
        {
            var desk = await _context.CheckInDesks.FindAsync(id);
            if (desk == null) return NotFound();

            return View(desk);
        }

        // post edit endpoint - Admin/CheckInDesks/Edit/2
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CheckInDesk desk)
        {
            if (id != desk.Id) return NotFound();

            desk.Terminal = (desk.Terminal ?? string.Empty).Trim().ToUpperInvariant();

            var exists = await _context.CheckInDesks
                .AnyAsync(d => d.Id != desk.Id &&
                               d.Terminal == desk.Terminal &&
                               d.DeskNumber == desk.DeskNumber);

            if (exists)
            {
                ModelState.AddModelError(string.Empty,
                    "A desk with this terminal and number already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(desk);
            }

            var existing = await _context.CheckInDesks
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);

            if (existing == null) return NotFound();

            desk.CreatedByUserId = existing.CreatedByUserId;

            try
            {
                _context.Update(desk);
                await _context.SaveChangesAsync();

                await LogAsync("Edit", "CheckInDesk", desk.Id,
                    $"Edited desk {desk.Terminal} / {desk.DeskNumber}");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await DeskExists(desk.Id))
                    return NotFound();

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/CheckInDesks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var desk = await _context.CheckInDesks.FindAsync(id);
            if (desk != null)
            {
                _context.CheckInDesks.Remove(desk);
                await _context.SaveChangesAsync();

                await LogAsync("Delete", "CheckInDesk", id,
                    $"Deleted desk {desk.Terminal} / {desk.DeskNumber}");
            }

            return RedirectToAction(nameof(Index));
        }

        private Task<bool> DeskExists(int id)
        {
            return _context.CheckInDesks.AnyAsync(d => d.Id == id);
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
