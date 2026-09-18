namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// suggested geo/date tag for a poem, pending moderation as part of a GanjoorPoemCorrection
    /// </summary>
    public class GanjoorPoemGeoDateTagCorrection
    {
        /// <summary>
        /// record id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// couplet index (matches PoemGeoDateTag.CoupletIndex)
        /// </summary>
        public int? CoupletIndex { get; set; }

        /// <summary>
        /// an existing, already approved location - set this OR the Suggested* fields below, not both
        /// </summary>
        public int? LocationId { get; set; }

        /// <summary>
        /// existing location (navigation)
        /// </summary>
        public virtual GanjoorGeoLocation Location { get; set; }

        /// <summary>
        /// name for a brand new, not yet approved location suggestion
        /// </summary>
        public string SuggestedLocationName { get; set; }

        /// <summary>
        /// latitude for a brand new, not yet approved location suggestion
        /// </summary>
        public double? SuggestedLatitude { get; set; }

        /// <summary>
        /// longitude for a brand new, not yet approved location suggestion
        /// </summary>
        public double? SuggestedLongitude { get; set; }

        /// <summary>
        /// lunar year (Hijri)
        /// </summary>
        public int? LunarYear { get; set; }

        /// <summary>
        /// lunar month (Hijri)
        /// </summary>
        public int? LunarMonth { get; set; }

        /// <summary>
        /// lunar day (Hijri)
        /// </summary>
        public int? LunarDay { get; set; }

        /// <summary>
        /// related person id (existing, approved GanjoorRelatedPerson only - no suggestion path for new people yet)
        /// </summary>
        public int? PersonId { get; set; }

        /// <summary>
        /// related person (navigation)
        /// </summary>
        public virtual GanjoorRelatedPerson Person { get; set; }

        /// <summary>
        /// if true, this tag is excluded from category/poet-level map aggregation (e.g. a place mentioned only
        /// for comparison, not actually visited/relevant to the poet's own path) - default false, matching
        /// PoemGeoDateTag.IgnoreInCategory
        /// </summary>
        public bool IgnoreInCategory { get; set; }

        /// <summary>
        /// contributor's own reasoning for this tag - especially important for indirect date references
        /// (e.g. an abjad-encoded year, or an allusion to a known historical event) where the connection
        /// to the suggested location/date isn't self-evident from the tag alone
        /// </summary>
        public string SuggestionNote { get; set; }

        /// <summary>
        /// true if this suggestion is for removing an existing PoemGeoDateTag rather than adding a new one
        /// </summary>
        public bool MarkForDelete { get; set; }

        /// <summary>
        /// id of the existing PoemGeoDateTag this suggestion would remove, when MarkForDelete is true
        /// </summary>
        public int? ExistingTagId { get; set; }

        /// <summary>
        /// review result
        /// </summary>
        public CorrectionReviewResult Result { get; set; }

        /// <summary>
        /// reviewer's note
        /// </summary>
        public string ReviewNote { get; set; }
    }
}
