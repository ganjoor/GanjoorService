using GanjooRazor.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GanjooRazor.Pages
{
    public class _SimiPartialViewModel : PageModel
    {
        public InlineSimilarPoems InlineSimilarPoems { get; set; }

        public string Rhythm { get; set; }

        public string RhymeLetters { get; set; }
    }
}
