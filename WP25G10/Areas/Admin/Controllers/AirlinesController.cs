using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
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
        private readonly IWebHostEnvironment _env;

        public AirlinesController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        public async Task<IActionResult> Index(
          string? search,
          string status = "all",
          string sort = "created_desc",
          int page = 1,
          int pageSize = 5)
        {
            var query = _context.Airlines.AsQueryable();

            // search
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();

                query = query.Where(a =>
                    a.Name.ToLower().Contains(s) ||
                    a.Code.ToLower().Contains(s) ||
                    (a.Country != null && a.Country.ToLower().Contains(s)));
            }

            // filter status
            switch (status)
            {
                case "active":
                    query = query.Where(a => a.IsActive);
                    break;
                case "inactive":
                    query = query.Where(a => !a.IsActive);
                    break;
            }

            // count after filters
            var totalCount = await query.CountAsync();

            if (page < 1) page = 1;
            var totalPages = (int)System.Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            // sort
            query = sort switch
            {
                "name_asc" => query.OrderBy(a => a.Name),
                "name_desc" => query.OrderByDescending(a => a.Name),

                "code_asc" => query.OrderBy(a => a.Code),
                "code_desc" => query.OrderByDescending(a => a.Code),

                // default
                _ => query.OrderByDescending(a => a.Id)
            };

            var airlines = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new AirlinesIndexViewModel
            {
                Airlines = airlines,
                SearchTerm = search,
                StatusFilter = status,
                SortOrder = sort,
                PageNumber = page,
                TotalPages = totalPages
            };

            return View(vm);
        }

        // details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var airline = await _context.Airlines
                .Include(a => a.CreatedByUser)
                .Include(a => a.Flights)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (airline == null) return NotFound();

            return View(airline);
        }



        // create airlines
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Airline airline, IFormFile? logoFile)
        {
            airline.CreatedByUserId = _userManager.GetUserId(User);

            if (logoFile != null && logoFile.Length > 0)
            {
                var relativePath = await SaveLogoFileAsync(logoFile);
                airline.LogoUrl = relativePath;
            }

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

        // edit arilines
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
            [Bind("Id,Name,Code,Country,LogoUrl,IsActive")] Airline airline,
            IFormFile? logoFile)
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

            // upload file override url
            if (logoFile != null && logoFile.Length > 0)
            {
                if (!string.IsNullOrEmpty(existing.LogoUrl) &&
                    existing.LogoUrl.StartsWith("/uploads/airlines/"))
                {
                    var oldPath = Path.Combine(_env.WebRootPath,
                        existing.LogoUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(oldPath))
                    {
                        System.IO.File.Delete(oldPath);
                    }
                }

                var relativePath = await SaveLogoFileAsync(logoFile);
                airline.LogoUrl = relativePath;
            }

            _context.Update(airline);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // dlete airlines
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
                // when new one delete local photos so wont overload
                if (!string.IsNullOrEmpty(airline.LogoUrl) &&
                    airline.LogoUrl.StartsWith("/uploads/airlines/"))
                {
                    var oldPath = Path.Combine(_env.WebRootPath,
                        airline.LogoUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(oldPath))
                    {
                        System.IO.File.Delete(oldPath);
                    }
                }

                _context.Airlines.Remove(airline);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool AirlineExists(int id)
        {
            return _context.Airlines.Any(e => e.Id == id);
        }

        private async Task<string> SaveLogoFileAsync(IFormFile logoFile)
        {
            // save uplaoded files in wwwroot/uploads/airlines
            var uploadsRootFolder = Path.Combine(_env.WebRootPath, "uploads", "airlines");
            Directory.CreateDirectory(uploadsRootFolder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(logoFile.FileName)}";
            var filePath = Path.Combine(uploadsRootFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await logoFile.CopyToAsync(stream);
            }

            return $"/uploads/airlines/{fileName}";
        }
    }
}
