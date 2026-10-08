using System.Collections.Generic;

namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// a person plus their kinship/affiliation edges, resolved with the other side's name - the
    /// read-only counterpart of the pending-suggestion PersonGraphSuggestion (see
    /// GanjoorPoemGeoDateTagCorrection.SuggestedPersonGraphJson), served by GET api/people/{id}/relations
    /// </summary>
    public class GanjoorPersonRelationsViewModel
    {
        /// <summary>
        /// the subject person
        /// </summary>
        public GanjoorRelatedPerson Person { get; set; }

        /// <summary>
        /// kinship edges touching Person (either side)
        /// </summary>
        public List<GanjoorPersonRelationInfo> Relations { get; set; }

        /// <summary>
        /// non-family ties touching Person (either side)
        /// </summary>
        public List<GanjoorPersonAffiliationInfo> Affiliations { get; set; }
    }

    /// <summary>
    /// one GanjoorPersonRelation row, resolved from Person's point of view
    /// </summary>
    public class GanjoorPersonRelationInfo
    {
        /// <summary>
        /// the underlying GanjoorPersonRelation row's own id - needed by the client to link to
        /// /User/SuggestPersonRelationEdit?relationId={id} for suggesting a change/removal of this specific edge
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// the other person in this relation
        /// </summary>
        public int OtherPersonId { get; set; }

        /// <summary>
        /// the other person's name (denormalized here so the client doesn't need a second lookup)
        /// </summary>
        public string OtherPersonName { get; set; }

        /// <summary>
        /// the kind of kinship edge
        /// </summary>
        public PersonRelationType RelationType { get; set; }

        /// <summary>
        /// see GanjoorPersonRelation.DegreeHint
        /// </summary>
        public int? DegreeHint { get; set; }

        /// <summary>
        /// free-text note
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        /// true if the subject person is Person1 in the underlying GanjoorPersonRelation row -
        /// needed by the client to render directional types (Parent, Ancestor) correctly
        /// </summary>
        public bool SubjectIsPerson1 { get; set; }

        /// <summary>
        /// couplets attesting this relation (see GanjoorPersonRelationEvidence), empty if none
        /// </summary>
        public List<GanjoorPersonRelationEvidenceInfo> Evidence { get; set; }
    }

    /// <summary>
    /// read-only projection of GanjoorPersonRelationEvidence
    /// </summary>
    public class GanjoorPersonRelationEvidenceInfo
    {
        /// <summary>
        /// evidence row id (what a RemoveEvidence suggestion refers to)
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// poem id
        /// </summary>
        public int PoemId { get; set; }

        /// <summary>
        /// couplet index inside the poem
        /// </summary>
        public int CoupletIndex { get; set; }

        /// <summary>
        /// couplet text snapshot
        /// </summary>
        public string CoupletText { get; set; }

        /// <summary>
        /// master category id (book) the poem belongs to
        /// </summary>
        public int MasterCatId { get; set; }

        /// <summary>
        /// master category title (filled by the server for display)
        /// </summary>
        public string MasterCatTitle { get; set; }

        /// <summary>
        /// true if guessed by a backfill rather than attached by a person (does not count for per-book filtering)
        /// </summary>
        public bool Inferred { get; set; }
    }

    /// <summary>
    /// one GanjoorPersonAffiliation row, resolved from Person's point of view - same shape/purpose
    /// as GanjoorPersonRelationInfo above, but for a non-family tie
    /// </summary>
    public class GanjoorPersonAffiliationInfo
    {
        /// <summary>
        /// the underlying GanjoorPersonAffiliation row's own id - used to link to
        /// /User/SuggestPersonRelationEdit?affiliationId={Id} for suggesting a change/removal of
        /// this edge, same role RelationId plays on GanjoorPersonRelationInfo
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// the other person in this tie
        /// </summary>
        public int OtherPersonId { get; set; }

        /// <summary>
        /// the other person's name
        /// </summary>
        public string OtherPersonName { get; set; }

        /// <summary>
        /// the kind of non-family tie
        /// </summary>
        public PersonAffiliationType AffiliationType { get; set; }

        /// <summary>
        /// free-text note
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        /// true if the subject person is Person1 in the underlying GanjoorPersonAffiliation row
        /// </summary>
        public bool SubjectIsPerson1 { get; set; }

        /// <summary>
        /// optional couplets attesting this tie (see GanjoorPersonAffiliationEvidence), empty if none
        /// </summary>
        public List<GanjoorPersonRelationEvidenceInfo> Evidence { get; set; }
    }

    /// <summary>
    /// a kinship relation that has no human-attached evidence couplet yet (inferred rows do not count) -
    /// one row of the "relations without evidence" worklist
    /// </summary>
    public class GanjoorRelationWithoutEvidence
    {
        /// <summary>
        /// relation id
        /// </summary>
        public int RelationId { get; set; }

        /// <summary>
        /// Person1 id
        /// </summary>
        public int Person1Id { get; set; }

        /// <summary>
        /// Person1 name
        /// </summary>
        public string Person1Name { get; set; }

        /// <summary>
        /// Person2 id
        /// </summary>
        public int Person2Id { get; set; }

        /// <summary>
        /// Person2 name
        /// </summary>
        public string Person2Name { get; set; }

        /// <summary>
        /// relation type
        /// </summary>
        public PersonRelationType RelationType { get; set; }

        /// <summary>
        /// degree hint (ancestor relations)
        /// </summary>
        public int? DegreeHint { get; set; }

        /// <summary>
        /// number of evidence suggestions for this relation still waiting for review
        /// </summary>
        public int PendingEvidenceSuggestions { get; set; }

        /// <summary>
        /// true when this row is a non-family affiliation (then RelationId is the affiliation's id and
        /// AffiliationType is set; RelationType is meaningless)
        /// </summary>
        public bool IsAffiliation { get; set; }

        /// <summary>
        /// the affiliation's type when IsAffiliation
        /// </summary>
        public PersonAffiliationType? AffiliationType { get; set; }
    }
}
