using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WP25G10.Models.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Collections.Generic;
using WP25G10.Security;


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

        private static bool IsUserActive(IdentityUser user)
        {
            return !user.LockoutEnd.HasValue || user.LockoutEnd <= DateTimeOffset.UtcNow;
        }

        private static List<string> GetFlightPermissionsFromModel(bool view, bool create, bool edit, bool delete)
        {
            var perms = new List<string>();

            if (view) perms.Add(Permissions.Flights.View);
            if (create) perms.Add(Permissions.Flights.Create);
            if (edit) perms.Add(Permissions.Flights.Edit);
            if (delete) perms.Add(Permissions.Flights.Delete);

            return perms;
        }

        private async Task ReplaceFlightPermissionClaimsAsync(IdentityUser user, List<string> newPerms)
        {
            var claims = await _userManager.GetClaimsAsync(user);

            var toRemove = claims
                .Where(c => c.Type == Permissions.ClaimType && c.Value.StartsWith("flights."))
                .ToList();

            foreach (var c in toRemove)
                await _userManager.RemoveClaimAsync(user, c);

            foreach (var p in newPerms.Distinct())
                await _userManager.AddClaimAsync(user, new Claim(Permissions.ClaimType, p));

            await _userManager.UpdateSecurityStampAsync(user);
        }

        public async Task<IActionResult> Index(
            string? search = null,
            string StatusFilter = "all",
            int page = 1,
            int pageSize = 10,
            bool reset = false)
        {
            var status = string.IsNullOrWhiteSpace(StatusFilter) ? "all" : StatusFilter;

            if (reset)
            {
                HttpContext.Session.Remove("Staff_Search");
                HttpContext.Session.Remove("Staff_Status");
                HttpContext.Session.Remove("Staff_Page");

                search = null;
                status = "all";
                page = 1;
            }
            else
            {
                var hasQuery =
                Request.Query.ContainsKey("search") ||
                Request.Query.ContainsKey("StatusFilter") ||
                Request.Query.ContainsKey("page");

                if (!hasQuery)
                {
                    search ??= HttpContext.Session.GetString("Staff_Search");
                    status = HttpContext.Session.GetString("Staff_Status") ?? status;

                    var storedPage = HttpContext.Session.GetInt32("Staff_Page");
                    if (storedPage.HasValue && storedPage.Value > 0)
                    {
                        page = storedPage.Value;
                    }
                }
            }
            HttpContext.Session.SetString("Staff_Search", search ?? string.Empty);
            HttpContext.Session.SetString("Staff_Status", status ?? "all");
            HttpContext.Session.SetInt32("Staff_Page", page);

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

        // get view endpoint - Admin/Staff/Create
        public IActionResult Create()
        {
            // default: view flights enabled
            return View(new StaffCreateViewModel { CanViewFlights = true });
        }

        // post endpoint per create - Admin/Staff/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StaffCreateViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (!await _roleManager.RoleExistsAsync("Staff"))
                await _roleManager.CreateAsync(new IdentityRole("Staff"));

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
                    ModelState.AddModelError(string.Empty, error.Description);

                return View(model);
            }

            var roleResult = await _userManager.AddToRoleAsync(user, "Staff");
            if (!roleResult.Succeeded)
            {
                foreach (var error in roleResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                return View(model);
            }

            var perms = GetFlightPermissionsFromModel(
                model.CanViewFlights,
                model.CanCreateFlights,
                model.CanEditFlights,
                model.CanDeleteFlights
            );

            if ((model.CanCreateFlights || model.CanEditFlights || model.CanDeleteFlights) && !model.CanViewFlights)
                perms.Insert(0, Permissions.Flights.View);

            await ReplaceFlightPermissionClaimsAsync(user, perms);

            return RedirectToAction(nameof(Index));
        }

        // per edit view consider permissions too
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var isStaff = await _userManager.IsInRoleAsync(user, "Staff");
            if (!isStaff) return NotFound();

            var claims = await _userManager.GetClaimsAsync(user);

            bool Has(string p) => claims.Any(c => c.Type == Permissions.ClaimType && c.Value == p);

            var vm = new StaffEditViewModel
            {
                Id = user.Id,
                Email = user.Email ?? "",
                UserName = user.UserName ?? "",
                IsActive = IsUserActive(user),

                CanViewFlights = Has(Permissions.Flights.View),
                CanCreateFlights = Has(Permissions.Flights.Create),
                CanEditFlights = Has(Permissions.Flights.Edit),
                CanDeleteFlights = Has(Permissions.Flights.Delete),
            };

            return View(vm);
        }

        // edit endpoint per staff - Admin/Staff/Edit/2
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, StaffEditViewModel model)
        {
            if (id != model.Id) return NotFound();
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var isStaff = await _userManager.IsInRoleAsync(user, "Staff");
            if (!isStaff) return NotFound();

            user.Email = model.Email;
            user.UserName = model.UserName;

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
                    ModelState.AddModelError(string.Empty, error.Description);

                return View(model);
            }

            var perms = GetFlightPermissionsFromModel(
                model.CanViewFlights,
                model.CanCreateFlights,
                model.CanEditFlights,
                model.CanDeleteFlights
            );

            // if create/edit/delete is checked nese view eshte e lejuar
            if ((model.CanCreateFlights || model.CanEditFlights || model.CanDeleteFlights) && !model.CanViewFlights)
                perms.Insert(0, Permissions.Flights.View);

            await ReplaceFlightPermissionClaimsAsync(user, perms);

            return RedirectToAction(nameof(Index));
        }

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
            await _userManager.UpdateSecurityStampAsync(user);

            return RedirectToAction(nameof(Index));
        }

        // delete staff endpoint = Admin/Staff/Delete/5
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

        // get endpoint details of the staff
        [HttpGet]
        public async Task<IActionResult> DetailsJson(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var isStaff = await _userManager.IsInRoleAsync(user, "Staff");
            if (!isStaff) return NotFound();

            var claims = await _userManager.GetClaimsAsync(user);

            bool Has(string perm) => claims.Any(c => c.Type == Permissions.ClaimType && c.Value == perm);

            var data = new
            {
                id = user.Id,
                userName = user.UserName ?? "",
                email = user.Email ?? "",
                isActive = IsUserActive(user),

                permissions = new
                {
                    canViewFlights = Has(Permissions.Flights.View),
                    canCreateFlights = Has(Permissions.Flights.Create),
                    canEditFlights = Has(Permissions.Flights.Edit),
                    canDeleteFlights = Has(Permissions.Flights.Delete)
                }
            };

            return Json(data);
        }
    }
}