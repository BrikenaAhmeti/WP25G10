using System.Collections.Generic;

namespace WP25G10.Models.ViewModels
{
    public class AirlinesIndexViewModel
    {
        public IEnumerable<Airline> Airlines { get; set; } = new List<Airline>();

        public string? SearchTerm { get; set; }
        public string StatusFilter { get; set; } = "all";

        public string SortOrder { get; set; } = "created_desc";

        public int PageNumber { get; set; }
        public int TotalPages { get; set; }

        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
