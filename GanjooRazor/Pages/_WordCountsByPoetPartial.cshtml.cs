using Microsoft.AspNetCore.Mvc.RazorPages;
using RMuseum.Models.Ganjoor.ViewModels;

namespace GanjooRazor.Pages
{
    public class _WordCountsByPoetPartialModel : PageModel
    {
        public string Term { get; set; }
        public PoetOrCatWordStat[]  WordStats { get; set; }
    }
}
