using System.Collections.Generic;
using WP25G10.Models;

namespace WP25G10.Models.ViewModels
{
    public class CheckInDesksIndexViewModel
    {
        public List<CheckInDesk> Desks { get; set; } = new();

        public string? SearchTerm { get; set; }

        public string StatusFilter { get; set; } = "all";

        public int PageNumber { get; set; }
        public int TotalPages { get; set; }

        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
