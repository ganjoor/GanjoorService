using Microsoft.AspNetCore.Mvc.RazorPages;
using RMuseum.Models.Ganjoor.ViewModels;

namespace GanjooRazor.Pages
{
    public class _CategoryWordsCountByCatPartialModel : PageModel
    {
        public string Term { get; set; }
        public PoetOrCatWordStat[] WordStats { get; set; }
        public bool Whole { get; set; }
        public int TotalCount { get; set; }
        public bool Blur { get; set; }

        public _CategoryWordsCountByCatTablePartialModel Model
        {
            get
            {
                return new _CategoryWordsCountByCatTablePartialModel()
                {
                    Term = Term,
                    WordStats = WordStats,
                    Whole = Whole,
                    TotalCount = TotalCount,
                    Blur = Blur
                };
            }
        }
    }
}
