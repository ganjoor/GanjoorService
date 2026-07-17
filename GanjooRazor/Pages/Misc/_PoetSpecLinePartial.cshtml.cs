using Microsoft.AspNetCore.Mvc.RazorPages;
using RMuseum.Models.Ganjoor.ViewModels;

namespace GanjooRazor.Pages
{
    public class _PoetSpecLinePartialModel : PageModel
    {
        public bool ModeratePoetPhotos { get; set; }
        public GanjoorPoetSuggestedSpecLineViewModel Line { get; set; }
    }
}
