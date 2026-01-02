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

        public CheckInDesksController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Admin/CheckInDesks
        public async Task<IActionResult> Index(
            string? search,
            string status = "all",
            int page = 1,
            int pageSize = 10)
        {
            var query = _context.CheckInDesks.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(d =>
                    d.Terminal.Contains(s) ||
                    d.DeskNumber.ToString().Contains(s));
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

            if (page < 1) page = 1;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var desks = await query
                .OrderBy(d => d.Terminal)
                .ThenBy(d => d.DeskNumber)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new CheckInDesksIndexViewModel
            {
                Desks = desks,
                SearchTerm = search,
                StatusFilter = status,
                PageNumber = page,
                TotalPages = totalPages
            };

            return View(vm);
        }

        // GET: Admin/CheckInDesks/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/CheckInDesks/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CheckInDesk desk)
        {
            // Normalize terminal
            desk.Terminal = (desk.Terminal ?? string.Empty).Trim().ToUpperInvariant();

            // Uniqueness: Terminal + DeskNumber
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

            desk.CreatedByUserId = _userManager.GetUserId(User)!;

            _context.CheckInDesks.Add(desk);
            await _context.SaveChangesAsync();

            await LogAsync("Create", "CheckInDesk", desk.Id,
                $"Created desk {desk.Terminal} / {desk.DeskNumber}");

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/CheckInDesks/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var desk = await _context.CheckInDesks.FindAsync(id);
            if (desk == null) return NotFound();

            return View(desk);
        }

        // POST: Admin/CheckInDesks/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CheckInDesk desk)
        {
            if (id != desk.Id) return NotFound();

            // Normalize
            desk.Terminal = (desk.Terminal ?? string.Empty).Trim().ToUpperInvariant();

            // Uniqueness
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

            // Preserve CreatedByUserId
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
