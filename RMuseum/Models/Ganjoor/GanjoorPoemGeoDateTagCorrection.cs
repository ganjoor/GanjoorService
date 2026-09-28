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
        /// related person id - an existing, already approved GanjoorRelatedPerson. Set this OR
        /// SuggestedPersonGraphJson below, not both (same pattern as LocationId/Suggested* above).
        /// </summary>
        public int? PersonId { get; set; }

        /// <summary>
        /// related person (navigation)
        /// </summary>
        public virtual GanjoorRelatedPerson Person { get; set; }

        /// <summary>
        /// a brand new, not yet approved person (and optionally that person's relatives/relations,
        /// which may themselves be new people) - serialized JSON rather than its own set of
        /// correction tables, because a single suggestion can introduce several interlinked new
        /// people at once (e.g. "add this person, and their father, and the relation between them")
        /// and a new person referencing another not-yet-existing new person has no real id to point
        /// at until the whole graph is approved together. Expected shape (local keys are only used
        /// to resolve relations within this same submission and never stored beyond approval time):
        /// {
        ///   "person": { "localKey": "p1", "existingPersonId": null, "name": "...", "description": "...",
        ///               "wikiUrl": "...", "birthYearInLHijri": null, "deathYearInLHijri": null,
        ///               "validBirthDate": false, "validDeathDate": false,
        ///               "birthLocationId": null, "deathLocationId": null,
        ///               "familyTreeCaption": null },
        ///   "relatedPeople": [ { "localKey": "p2", "existingPersonId": 42, ... } ],
        ///   "relations": [ { "kind": "family", "person1": "p1", "person2": "p2", "relationType": "Parent",
        ///                     "degreeHint": null, "note": "..." },
        ///                   { "kind": "affiliation", "person1": "p2", "person2": "p3",
        ///                     "affiliationType": "Minister", "note": "..." } ]
        /// }
        /// "person" is the node that ends up assigned to PersonId once approved. Only set when
        /// PersonId above is null. Each entry in "relations" carries a "kind" discriminator so one
        /// submission can suggest both kinship edges (materialized as GanjoorPersonRelation,
        /// "relationType" against PersonRelationType) and non-family ties (materialized as
        /// GanjoorPersonAffiliation, "affiliationType" against PersonAffiliationType) at once - e.g.
        /// introducing a person along with both their father and the king they served.
        /// </summary>
        public string SuggestedPersonGraphJson { get; set; }

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
