using System;

namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// one piece of (optional) evidence for a GanjoorPersonAffiliation: a specific couplet of a
    /// specific poem that states or implies the tie (e.g. a poem saying who killed whom).
    /// Same idea as GanjoorPersonRelationEvidence; PoemId/MasterCatId are plain ints (no FK) and
    /// CoupletText is a snapshot so re-importing poems never blocks or erases evidence.
    /// </summary>
    public class GanjoorPersonAffiliationEvidence
    {
        /// <summary>
        /// record id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// the affiliation this evidence supports (deleting the affiliation deletes its evidence)
        /// </summary>
        public int AffiliationId { get; set; }

        /// <summary>
        /// affiliation (navigation)
        /// </summary>
        public virtual GanjoorPersonAffiliation Affiliation { get; set; }

        /// <summary>
        /// poem the couplet belongs to (not an FK)
        /// </summary>
        public int PoemId { get; set; }

        /// <summary>
        /// couplet index inside the poem (0-based)
        /// </summary>
        public int CoupletIndex { get; set; }

        /// <summary>
        /// snapshot of the couplet text at the time the evidence was added
        /// </summary>
        public string CoupletText { get; set; }

        /// <summary>
        /// the poem's master category (see GanjoorPersonRelationEvidence.MasterCatId)
        /// </summary>
        public int MasterCatId { get; set; }

        /// <summary>
        /// user who suggested this evidence
        /// </summary>
        public Guid? AddedByUserId { get; set; }

        /// <summary>
        /// date added
        /// </summary>
        public DateTime DateAdded { get; set; }
    }
}
