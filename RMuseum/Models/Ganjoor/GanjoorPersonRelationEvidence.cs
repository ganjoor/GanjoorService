using System;

namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// one piece of evidence ("attested in") for a GanjoorPersonRelation: a specific couplet of a
    /// specific poem that states or implies the relation. A relation may have any number of these.
    /// Evidence rows are what lets the same pair of people have different relations in different
    /// books (e.g. different editions of the Shahnameh) - each row remembers the "master category"
    /// (see MasterCatId) of its poem, so a view can be limited to relations attested in one book.
    /// PoemId/MasterCatId are deliberately plain ints (no FK) and CoupletText is a snapshot, so
    /// re-importing or deleting poems/categories never blocks or silently erases evidence.
    /// </summary>
    public class GanjoorPersonRelationEvidence
    {
        /// <summary>
        /// record id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// the relation this evidence supports (deleting the relation deletes its evidence)
        /// </summary>
        public int RelationId { get; set; }

        /// <summary>
        /// relation (navigation)
        /// </summary>
        public virtual GanjoorPersonRelation Relation { get; set; }

        /// <summary>
        /// poem the couplet belongs to (not an FK, see class remarks)
        /// </summary>
        public int PoemId { get; set; }

        /// <summary>
        /// couplet index inside the poem, same convention as PoemGeoDateTag.CoupletIndex
        /// </summary>
        public int CoupletIndex { get; set; }

        /// <summary>
        /// snapshot of the couplet text at the time the evidence was added
        /// </summary>
        public string CoupletText { get; set; }

        /// <summary>
        /// the poem's master category: the first category, walking up from the poem's own category,
        /// whose CatType is Book; or the topmost category (ParentId == null) if none is a Book.
        /// Computed when the evidence is added (not an FK, see class remarks).
        /// </summary>
        public int MasterCatId { get; set; }

        /// <summary>
        /// true for rows guessed by a backfill script rather than attached by a person. Inferred
        /// rows are never counted when filtering a view by master category - see
        /// GanjoorPersonRelationService docs; only a moderator-confirmed (Inferred == false) row counts.
        /// </summary>
        public bool Inferred { get; set; }

        /// <summary>
        /// user who suggested this evidence (null for backfilled rows)
        /// </summary>
        public Guid? AddedByUserId { get; set; }

        /// <summary>
        /// date added
        /// </summary>
        public DateTime DateAdded { get; set; }
    }
}
