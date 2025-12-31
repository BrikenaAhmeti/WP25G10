using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WP25G10.Models.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;


namespace WP25G10.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class StaffController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public StaffController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // Helper: active = not locked or lockout in the past
        private static bool IsUserActive(IdentityUser user)
        {
            return !user.LockoutEnd.HasValue || user.LockoutEnd <= DateTimeOffset.UtcNow;
        }

        public async Task<IActionResult> Index(
         string? search = null,
         string StatusFilter = "all",
         int page = 1,
         int pageSize = 10)
        {
            var status = string.IsNullOrWhiteSpace(StatusFilter) ? "all" : StatusFilter;

            var staffUsers = await _userManager.GetUsersInRoleAsync("Staff");
            var query = staffUsers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(u =>
                    (!string.IsNullOrEmpty(u.UserName) && u.UserName.ToLower().Contains(s)) ||
                    (!string.IsNullOrEmpty(u.Email) && u.Email.ToLower().Contains(s)));
            }

            switch (status)
            {
                case "active":
                    query = query.Where(u => IsUserActive(u));
                    break;
                case "inactive":
                    query = query.Where(u => !IsUserActive(u));
                    break;
            }

            var totalCount = query.Count();

            if (page < 1) page = 1;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var pagedUsers = query
                .OrderBy(u => u.UserName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var staffList = pagedUsers
                .Select(u => new StaffListItemViewModel
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Email = u.Email,
                    IsActive = IsUserActive(u)
                })
                .ToList();

            var vm = new StaffIndexViewModel
            {
                Staff = staffList,
                SearchTerm = search,
                StatusFilter = status,
                PageNumber = page,
                TotalPages = totalPages
            };

            return View(vm);
        }

        // GET: Admin/Staff/Create
        public IActionResult Create()
        {
            return View(new StaffCreateViewModel());
        }

        // POST: Admin/Staff/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StaffCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Ensure Staff role exists
            if (!await _roleManager.RoleExistsAsync("Staff"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Staff"));
            }

            var user = new IdentityUser
            {
                UserName = model.UserName,
                Email = model.Email,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user, model.Password);
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            var roleResult = await _userManager.AddToRoleAsync(user, "Staff");
            if (!roleResult.Succeeded)
            {
                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Staff/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var isStaff = await _userManager.IsInRoleAsync(user, "Staff");
            if (!isStaff) return NotFound(); // don't edit non-staff here

            var vm = new StaffEditViewModel
            {
                Id = user.Id,
                Email = user.Email ?? "",
                UserName = user.UserName ?? "",
                IsActive = IsUserActive(user)
            };

            return View(vm);
        }

        // POST: Admin/Staff/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, StaffEditViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var isStaff = await _userManager.IsInRoleAsync(user, "Staff");
            if (!isStaff) return NotFound();

            user.Email = model.Email;
            user.UserName = model.UserName;

            // Active/Inactive via Lockout
            if (model.IsActive)
            {
                user.LockoutEnd = null;
                user.LockoutEnabled = false;
            }
            else
            {
                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.MaxValue;
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Staff/ToggleActive
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var isStaff = await _userManager.IsInRoleAsync(user, "Staff");
            if (!isStaff) return NotFound();

            if (IsUserActive(user))
            {
                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.MaxValue;
            }
            else
            {
                user.LockoutEnabled = false;
                user.LockoutEnd = null;
            }

            await _userManager.UpdateAsync(user);

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Staff/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var isStaff = await _userManager.IsInRoleAsync(user, "Staff");
            if (!isStaff) return NotFound();

            await _userManager.DeleteAsync(user);

            return RedirectToAction(nameof(Index));
        }
    }
}
