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
    }
}
