using Microsoft.AspNetCore.Mvc.RazorPages;
using RMuseum.Models.Generic.ViewModels;

namespace GanjooRazor.Pages
{
    public class _GroupedByDateViewTablePartialModel : PageModel
    {
        public GroupedByDateViewModel[] Array { get; set; }
    }
}
