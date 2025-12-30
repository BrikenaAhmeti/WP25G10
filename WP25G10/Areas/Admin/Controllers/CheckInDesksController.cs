using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WP25G10.Data;
using WP25G10.Models;

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
        public async Task<IActionResult> Index(string? terminal, int page = 1, int pageSize = 10)
        {
            var query = _context.CheckInDesks.AsQueryable();

            if (!string.IsNullOrWhiteSpace(terminal))
            {
                query = query.Where(d => d.Terminal.Contains(terminal));
            }

            var totalItems = await query.CountAsync();
            var desks = await query
                .OrderBy(d => d.Terminal)
                .ThenBy(d => d.DeskNumber)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TerminalFilter = terminal;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;

            return View(desks);
        }
        public async Task<IActionResult> Details(int id)
        {
            var desk = await _context.CheckInDesks
                .FirstOrDefaultAsync(d => d.Id == id);

            if (desk == null)
            {
                return NotFound();
            }

            return View(desk);
        }
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CheckInDesk desk)
        {
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

        public async Task<IActionResult> Edit(int id)
        {
            var desk = await _context.CheckInDesks.FindAsync(id);
            if (desk == null)
            {
                return NotFound();
            }

            return View(desk);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CheckInDesk desk)
        {
            if (id != desk.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(desk);
            }

            try
            {
                _context.Entry(desk).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                await LogAsync("Edit", "CheckInDesk", desk.Id,
                    $"Edited desk {desk.Terminal} / {desk.DeskNumber}");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await DeskExists(desk.Id))
                {
                    return NotFound();
                }
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var desk = await _context.CheckInDesks
                .FirstOrDefaultAsync(d => d.Id == id);

            if (desk == null)
            {
                return NotFound();
            }

            return View(desk);
        }

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