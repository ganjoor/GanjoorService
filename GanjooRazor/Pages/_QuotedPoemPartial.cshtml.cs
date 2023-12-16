using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RMuseum.Models.Ganjoor;

namespace GanjooRazor.Pages
{
    public class _QuotedPoemPartialModel : PageModel
    {
        public GanjoorQuotedPoem GanjoorQuotedPoem { get; set; }

        public string PoetImageUrl { get; set; }

        public string PoetNickName { get; set; }
    }
}
