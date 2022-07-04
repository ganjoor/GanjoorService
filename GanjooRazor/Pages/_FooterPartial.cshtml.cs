using Microsoft.AspNetCore.Mvc.RazorPages;
using RMuseum.Models.Ganjoor.ViewModels;

namespace GanjooRazor.Pages
{
    public class _FooterPartialModel : PageModel
    {
        public bool StickyEnabled { get; set; }

        public GanjoorPageCompleteViewModel GanjoorPage { get; set; }
    }
}
