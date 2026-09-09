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
    }

    public class SemanticSearchResultDto
    {
        public int PoemId { get; set; }
        public string Title { get; set; }
        public string FullTitle { get; set; }
        public string FullUrl { get; set; }
        public string PoemSummary { get; set; }

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
    }
}
