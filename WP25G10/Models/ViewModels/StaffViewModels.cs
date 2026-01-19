using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace WP25G10.Models.ViewModels
{
    public class StaffListItemViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Display(Name = "User name")]
        public string? UserName { get; set; }

        [Display(Name = "Email")]
        public string? Email { get; set; }

        public bool IsActive { get; set; }
    }

    public class StaffCreateViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "User name")]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Can view flights")]
        public bool CanViewFlights { get; set; } = true;

        [Display(Name = "Can create flights")]
        public bool CanCreateFlights { get; set; } = false;

        [Display(Name = "Can edit flights")]
        public bool CanEditFlights { get; set; } = false;

        [Display(Name = "Can delete flights")]
        public bool CanDeleteFlights { get; set; } = false;
    }

    public class StaffEditViewModel
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "User name")]
        public string UserName { get; set; } = string.Empty;

        [Display(Name = "Active")]
        public bool IsActive { get; set; }

        // ✅ Dynamic permissions (Claims)
        [Display(Name = "Can view flights")]
        public bool CanViewFlights { get; set; } = true;

        [Display(Name = "Can create flights")]
        public bool CanCreateFlights { get; set; } = false;

        [Display(Name = "Can edit flights")]
        public bool CanEditFlights { get; set; } = false;

        [Display(Name = "Can delete flights")]
        public bool CanDeleteFlights { get; set; } = false;
    }

    public class StaffIndexViewModel
    {
        public List<StaffListItemViewModel> Staff { get; set; } = new();

        public string? SearchTerm { get; set; }
        public string StatusFilter { get; set; } = "all";

        public int PageNumber { get; set; }
        public int TotalPages { get; set; }

        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}