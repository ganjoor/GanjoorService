using Microsoft.AspNetCore.Mvc.RazorPages;
using RMuseum.Models.Ganjoor.ViewModels;

namespace GanjooRazor.Pages
{
    public class _MultipleQuotedPoemsPartialModel : PageModel
    {
        public GanjoorQuotedPoemViewModel[] GanjoorQuotedPoems { get; set; }

        public string PoetImageUrl { get; set; }

        public string PoetNickName { get; set; }

        public bool CanEdit { get; set; }

        public _QuotedPoemPartialModel GetQuotedPoemModel(GanjoorQuotedPoemViewModel quotedPoem, string poetImageUrl, string poetNickName)
        {
            return new _QuotedPoemPartialModel()
            {
                GanjoorQuotedPoemViewModel = quotedPoem,
                PoetImageUrl = poetImageUrl,
                PoetNickName = poetNickName,
                CanEdit = CanEdit,
            };
        }
    }
}
