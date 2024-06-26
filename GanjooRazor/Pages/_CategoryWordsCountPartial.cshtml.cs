using Microsoft.AspNetCore.Mvc.RazorPages;
using RMuseum.Models.Ganjoor;

namespace GanjooRazor.Pages
{
    public class _CategoryWordsCountPartialModel : PageModel
    {
        /// <summary>
        /// Cat Id
        /// </summary>
        public int CatId { get; set; }

        /// <summary>
        /// PoetId
        /// </summary>
        public int PoetId { get; set; }

        /// <summary>
        /// unique word count
        /// </summary>
        public int UniqueWordCount { get; set; }

        /// <summary>
        /// total word count
        /// </summary>
        public int TotalWordCount { get; set; }

        /// <summary>
        /// words counts
        /// </summary>
        public CategoryWordCount[] WordCounts { get; set; }

        public _CategoryWordsCountTablePartialModel TableModel
        {
            get
            {
                return new _CategoryWordsCountTablePartialModel()
                {
                    CatId = CatId,
                    PoetId = PoetId,
                    WordCounts = WordCounts
                };
            }
        }
    }
}
