using Microsoft.AspNetCore.Mvc.RazorPages;
using RMuseum.Models.Generic.ViewModels;
using RSecurityBackend.Models.Generic;

namespace GanjooRazor.Pages
{
    public class _GroupedByDateViewTablePartialModel : PageModel
    {
        public GroupedByDateViewModel[] Days { get; set; }
        public PaginationMetadata DaysPagination { get; set; }
        public GroupedByUserViewModel[] Users { get; set; }
        public PaginationMetadata UsersPagination { get; set; }
    }
}
