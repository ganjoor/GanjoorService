using Microsoft.AspNetCore.Mvc.RazorPages;
using RMuseum.Models.Ganjoor;

namespace GanjooRazor.Pages
{
    public class _MultipleQuotedPoemsPartialModel : PageModel
    {
        public GanjoorQuotedPoem[] GanjoorQuotedPoems { get; set; }

        public string PoetImageUrl { get; set; }

        public string PoetNickName { get; set; }

        public bool CanEdit { get; set; }

        public _QuotedPoemPartialModel GetQuotedPoemModel(GanjoorQuotedPoem quotedPoem, string poetImageUrl, string poetNickName)
        {
            return new _QuotedPoemPartialModel()
            {
                GanjoorQuotedPoem = quotedPoem,
                PoetImageUrl = poetImageUrl,
                PoetNickName = poetNickName,
                CanEdit = CanEdit,
            };
        }
    }
}
