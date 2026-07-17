using Microsoft.AspNetCore.Mvc.RazorPages;
using RMuseum.Models.Ganjoor.ViewModels;

namespace GanjooRazor.Pages
{
    public class _QuotedPoemPartialModel : PageModel
    {
        public GanjoorQuotedPoemViewModel GanjoorQuotedPoemViewModel { get; set; }
        public string PoetImageUrl { get; set; }
        public string PoetNickName { get; set; }
        public string BlockClass
        {
            get
            {
                return GanjoorQuotedPoemViewModel.ClaimedByBothPoets ? "inlinesimi ribbon-parent" : "inlinesimi";
            }
        }
        public bool CanEdit { get; set; }
    }
}
