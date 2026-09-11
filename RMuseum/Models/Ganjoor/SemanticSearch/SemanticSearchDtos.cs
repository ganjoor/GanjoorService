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

        /// <summary>
        /// Skips auto-detection entirely for this request — the "search globally instead"
        /// escape hatch. Needed because substring-based detection is genuinely ambiguous
        /// sometimes: "شمع و پروانه" is both a common poetic theme AND the literal title of a
        /// book by a specific poet, so a query using it as a theme could get silently locked to
        /// that one book with no way out otherwise. An explicit PoetId/CatId still applies even
        /// with this set — that's a deliberate scope the caller asked for, not something guessed.
        /// </summary>
        public bool DisableScopeDetection { get; set; }
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

        /// <summary>
        /// Id of the SemanticSearchQueryLog row written for this request, or null if logging
        /// itself failed (best-effort — see SemanticSearchService.SearchAsync — a logging
        /// failure must never fail the search itself). The UI passes this back with
        /// POST search/semantic/click if/when a result gets clicked, so the click can be
        /// correlated to the search that produced it.
        /// </summary>
        public int? LogId { get; set; }
    }

    /// <summary>
    /// Reported by the UI, fire-and-forget, if and when a person actually clicks through to one
    /// of the results — see SemanticSearchQueryLog for what this updates.
    /// </summary>
    public class SemanticSearchClickDto
    {
        public int LogId { get; set; }
        public int PoemId { get; set; }

        /// <summary>1-based position of the clicked result in the list that was returned</summary>
        public int Rank { get; set; }
    }
}
