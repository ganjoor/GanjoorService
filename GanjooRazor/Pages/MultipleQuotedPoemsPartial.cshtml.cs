using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;

namespace GanjooRazor.Pages
{
    public class MultipleQuotedPoemsPartialModel : PageModel
    {
        public GanjoorQuotedPoem[] GanjoorQuotedPoems { get; set; }

        public string PoetImageUrl { get; set; }

        public string PoetNickName { get; set; }

        public _QuotedPoemPartialModel GetQuotedPoemModel(GanjoorQuotedPoem quotedPoem, string poetImageUrl, string poetNickName)
        {
            return new _QuotedPoemPartialModel()
            {
                GanjoorQuotedPoem = quotedPoem,
                PoetImageUrl = poetImageUrl,
                PoetNickName = poetNickName
            };
        }
    }
}
