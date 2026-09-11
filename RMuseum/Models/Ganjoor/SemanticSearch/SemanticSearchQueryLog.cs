using System;

namespace RMuseum.Models.Ganjoor.SemanticSearch
{
    /// <summary>
    /// A lightweight, privacy-conscious log of semantic search usage — query text and result
    /// metadata only. Deliberately NO user identifiers: no IP address, no user agent, no
    /// session/user id. Exists so ranking decisions (the AI-summary down-ranking, a future
    /// couplet-level search, anything else) can be evaluated against real usage instead of a
    /// handful of manually spot-checked queries.
    /// </summary>
    public class SemanticSearchQueryLog
    {
        public int Id { get; set; }
        public string Query { get; set; }
        public DateTime DateTimeUtc { get; set; }
        public int RequestedTopK { get; set; }
        public int ResultCount { get; set; }
        public float? TopResultScore { get; set; }
        public bool ScopeDetected { get; set; }
        public string DetectedPoetName { get; set; }
        public string DetectedCategoryName { get; set; }
        public bool ScopeDetectionDisabled { get; set; }

        // Filled in later, via POST search/semantic/click, if and only if a result actually gets
        // clicked. Null/null/null (the expected common case — most searches presumably end
        // without a click, or the person is just browsing results) is itself useful signal, not
        // missing data.
        public int? ClickedPoemId { get; set; }
        public int? ClickedResultRank { get; set; }
        public DateTime? ClickedAtUtc { get; set; }
    }
}
