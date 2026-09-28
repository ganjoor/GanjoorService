namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// an approved kinship edge between two people (GanjoorRelatedPerson rows) - the live/materialized
    /// counterpart of a pending suggestion, which travels as JSON on GanjoorPoemGeoDateTagCorrection's
    /// SuggestedPersonGraphJson until it's approved and turned into rows here. The graph as a whole
    /// (all rows in this table) is a general kinship graph, not a strict tree - see PersonRelationType.
    /// </summary>
    public class GanjoorPersonRelation
    {
        /// <summary>
        /// record id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// first person in the relation - for a directional relation type (Parent, Ancestor) this is
        /// the parent/ancestor side; for a symmetric one (Sibling, Spouse) order doesn't matter
        /// </summary>
        public int Person1Id { get; set; }

        /// <summary>
        /// first person (navigation)
        /// </summary>
        public virtual GanjoorRelatedPerson Person1 { get; set; }

        /// <summary>
        /// second person in the relation - for a directional relation type (Parent, Ancestor) this is
        /// the child/descendant side
        /// </summary>
        public int Person2Id { get; set; }

        /// <summary>
        /// second person (navigation)
        /// </summary>
        public virtual GanjoorRelatedPerson Person2 { get; set; }

        /// <summary>
        /// the kind of relation between Person1 and Person2
        /// </summary>
        public PersonRelationType RelationType { get; set; }

        /// <summary>
        /// for RelationType == Ancestor, an optional known exact degree (e.g. 2 for "grandparent",
        /// 3 for "great-grandparent") - left null when only the relative order is known, not the
        /// exact number of generations in between. Not used for other relation types.
        /// </summary>
        public int? DegreeHint { get; set; }

        /// <summary>
        /// free-text note (e.g. sourcing/reasoning for this relation)
        /// </summary>
        public string Note { get; set; }
    }
}
