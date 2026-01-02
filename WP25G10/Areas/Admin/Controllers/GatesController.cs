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
    public class GatesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public GatesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Admin/Gates
        public async Task<IActionResult> Index()
        {
            var gates = await _context.Gates
                .OrderBy(g => g.Terminal)
                .ThenBy(g => g.Code)
                .ToListAsync();

            return View(gates);
        }

        // GET: /Admin/Gates/Create
        public IActionResult Create() => View();

        // POST: /Admin/Gates/Create
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

        // GET: /Admin/Gates/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var gate = await _context.Gates.FindAsync(id);
            if (gate == null) return NotFound();

            return View(gate);
        }

        // POST: /Admin/Gates/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Gate gate)
        {
            if (id != gate.Id) return BadRequest();

            if (!ModelState.IsValid) return View(gate);

            // Merr ekzistuesen (për me ruajt CreatedByUserId)
            var existing = await _context.Gates.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id);
            if (existing == null) return NotFound();

            gate.CreatedByUserId = existing.CreatedByUserId;

            _context.Update(gate);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Gates/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var gate = await _context.Gates.FirstOrDefaultAsync(g => g.Id == id);
            if (gate == null) return NotFound();

            return View(gate);
        }

        // POST: /Admin/Gates/Delete/5
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
