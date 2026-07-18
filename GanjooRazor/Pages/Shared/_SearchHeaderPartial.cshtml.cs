using RMuseum.Models.Ganjoor.ViewModels;
using System.Collections.Generic;

namespace GanjooRazor.Pages
{
    /// <summary>
    /// Model for the shared #hdr2 search-form + poet/category dropdown header used by GanjoorPage,
    /// Search, Simi, Quotes, Hashieha, Contribs, and FAQ. Before this, each page carried its own
    /// copy of this block - identical in 3 cases (Simi/Hashieha/Contribs), and varying in specific,
    /// explainable ways in the other 4 (see each page's own model-construction code for the mapping).
    /// </summary>
    public class _SearchHeaderPartialModel
    {
        public List<GanjoorPoetViewModel> Poets { get; set; }

        /// <summary>0 marks the "all poets" option as selected.</summary>
        public int SelectedPoetId { get; set; }

        public string SearchQuery { get; set; } = "";

        /// <summary>
        /// Whether the hidden "es" (exact search) field renders at all. Its value is always "1" when
        /// shown - only Search.cshtml ties this to a real toggle; every other page always shows it
        /// unconditionally.
        /// </summary>
        public bool ShowExactSearchHiddenField { get; set; } = true;

        /// <summary>Null (default) hides the category dropdown entirely.</summary>
        public GanjoorCatViewModel CurrentCategory { get; set; }

        /// <summary>
        /// Null means "always mark CurrentCategory as selected" (GanjoorPage/Simi/Hashieha/Contribs
        /// behavior - these never let you pick a different category than the one you're already
        /// looking at). A value means "mark whichever ancestor/current/child matches this id instead"
        /// (Search's behavior, where the category filter is independent of what you're looking at).
        /// </summary>
        public int? SelectedCategoryId { get; set; }

        /// <summary>
        /// GanjoorPage's category select is missing the inline width style the other 6 pages have -
        /// preserved here rather than silently unified, since it may be an intentional visual
        /// difference (or CSS already handles #cat's width and the inline style is redundant on the
        /// other pages - not something to guess at).
        /// </summary>
        public bool WideCategorySelect { get; set; } = true;
    }
}
