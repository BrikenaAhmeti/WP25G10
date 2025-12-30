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

        public async Task<IActionResult> Index()
        {
            var gates = await _context.Gates.OrderBy(g => g.Terminal).ThenBy(g => g.Code).ToListAsync();
            return View(gates);
        }

        public IActionResult Create() => View();

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

        // Add Edit, Delete similar to Airlines pattern
    }
}
