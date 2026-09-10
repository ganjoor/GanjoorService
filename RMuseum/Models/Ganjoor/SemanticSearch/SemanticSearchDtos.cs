using System.Collections.Generic;

namespace RMuseum.Models.Ganjoor.SemanticSearch
{
    /// <summary>
    /// "find a poem about..." request
    /// </summary>
    public class SemanticSearchRequestDto
    {
        public string Query { get; set; }

        /// <summary>
        /// how many results to return; a sane default is applied server-side if omitted/invalid
        /// </summary>
        public int? TopK { get; set; }

        /// <summary>
        /// Optional explicit scope, for a future UI (e.g. a poet picker) that wants to restrict
        /// search without relying on auto-detection from the query text. Auto-detection (see
        /// SemanticSearchService.DetectQueryScope) still runs even when these are set — an
        /// explicit PoetId/CatId narrows further, it doesn't replace detection.
        /// </summary>
        public int? PoetId { get; set; }
        public int? CatId { get; set; }
    }

    public class SemanticSearchVerseDto
    {
        public int VOrder { get; set; }

        /// <summary>
        /// matches GanjoorVerse.VersePosition.ToString() (e.g. "Right"/"Left") - same convention
        /// already used by the main ganjoor-data export, so any existing hemistich-pairing
        /// frontend code (see mini-ganjoor's renderVerses) can be reused as-is.
        /// </summary>
        public string Position { get; set; }
        public string Text { get; set; }
    }

    public class SemanticSearchResultDto
    {
        public int PoemId { get; set; }
        public string Title { get; set; }
        public string FullTitle { get; set; }
        public string FullUrl { get; set; }
        public string PoemSummary { get; set; }

        /// <summary>
        /// A short preview from the poem's actual text - the first couple of couplets, in
        /// original verse order. Not the whole poem; just enough for a result card.
        /// </summary>
        public List<SemanticSearchVerseDto> Verses { get; set; } = new List<SemanticSearchVerseDto>();

        /// <summary>
        /// cosine similarity, 0..1 for these normalized vectors (in practice results cluster in
        /// a narrower band - this is a relative ranking signal, not a calibrated probability)
        /// </summary>
        public float Score { get; set; }
    }

    public class SemanticSearchResponseDto
    {
        public string Query { get; set; }
        public List<SemanticSearchResultDto> Results { get; set; } = new List<SemanticSearchResultDto>();

        /// <summary>
        /// If the query text was recognized as referring to a specific poet and/or
        /// category/book (e.g. "در کدام شعر حافظ" -> حافظ, "در کدام بخش شاهنامه" -> شاهنامه),
        /// search was restricted to that scope and these are populated so the UI can show the
        /// user what was detected ("نتایج محدود به: حافظ") rather than silently filtering.
        /// Null/null if nothing was detected (or an explicit PoetId/CatId narrowed things
        /// without any name being recognized in the free text).
        /// </summary>
        public string DetectedPoetName { get; set; }
        public string DetectedCategoryName { get; set; }
    }
}

