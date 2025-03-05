using Microsoft.AspNetCore.Mvc.RazorPages;
using RMuseum.Models.Ganjoor.ViewModels;

namespace GanjooRazor.Pages
{
    public class _WordCountsByPoetModel : PageModel
    {
        public string Term { get; set; }
        public PoetOrCatWordStat[]  WordStats { get; set; }
    }
}
