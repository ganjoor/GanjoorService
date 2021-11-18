using Microsoft.AspNetCore.Mvc.RazorPages;
using RMuseum.Models.Ganjoor;

namespace GanjooRazor.Pages
{
    public class _SimiPartialViewModel : PageModel
    {
        public GanjoorCachedRelatedPoem[] RelatedPoems { get; set; }

        public string Rhythm { get; set; }

        public string RhymeLetters { get; set; }

        public int Skip { get; set; }

        public int PoemId { get; set; }
        public string PoemFullUrl { get; set; }
    }
}
