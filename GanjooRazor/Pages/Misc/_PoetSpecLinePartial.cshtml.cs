using Microsoft.AspNetCore.Mvc.RazorPages;
using RMuseum.Models.Ganjoor.ViewModels;

namespace GanjooRazor.Pages
{
    public class _PoetSpecLinePartialModel : PageModel
    {
        public bool ModeratePoetPhotos { get; set; }
        public GanjoorPoetSuggestedSpecLineViewModel Line { get; set; }

        /// <summary>
        /// set (alongside Line.Contents holding the error message, when Line.Id == 0) when the
        /// error is the "sanitizing had to drop real text" case - see
        /// GanjooRazor.Pages._CommentPartialModel.SanitizerRemainingText for the same pattern.
        /// </summary>
        public string SanitizerRemainingText { get; set; }
    }
}
