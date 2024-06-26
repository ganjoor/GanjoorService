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
        /// words counts
        /// </summary>
        public CategoryWordCount[] WordCounts { get; set; }
    }
}
