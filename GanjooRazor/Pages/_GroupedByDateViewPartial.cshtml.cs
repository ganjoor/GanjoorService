using Microsoft.AspNetCore.Mvc.RazorPages;
using RMuseum.Models.Generic.ViewModels;

namespace GanjooRazor.Pages
{
    public class _GroupedByDateViewPartialModel : PageModel
    {
        public GroupedByDateViewModel[] Days { get; set; }

        public _GroupedByDateViewTablePartialModel TableModel
        {
            get
            {
                return new _GroupedByDateViewTablePartialModel()
                {
                    Days = Days
                };
            }
        }
    }
}
