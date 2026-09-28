namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// an approved non-family tie between two people (GanjoorRelatedPerson rows) - e.g. a minister
    /// serving a king. Kept separate from GanjoorPersonRelation (kinship) rather than folded into
    /// it: this is what lets two unrelated family trees show up as adjacent to each other (e.g.
    /// browsing the Barmakid tree surfaces a link out to the Abbasid tree via a shared minister)
    /// without treating "family tree" as its own entity to be linked - the tie is between the two
    /// people, and the tree-to-tree adjacency is just what falls out of rendering it that way.
    /// </summary>
    public class GanjoorPersonAffiliation
    {
        /// <summary>
        /// record id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// first person in the tie - for a directional type (Minister, Advisor, Courtier, Patron)
        /// this is the one in the subordinate/serving role; for a symmetric one (Ally, Rival) order
        /// doesn't matter
        /// </summary>
        public int Person1Id { get; set; }

        /// <summary>
        /// first person (navigation)
        /// </summary>
        public virtual GanjoorRelatedPerson Person1 { get; set; }

        /// <summary>
        /// second person in the tie - for a directional type this is the one being served
        /// </summary>
        public int Person2Id { get; set; }

        /// <summary>
        /// second person (navigation)
        /// </summary>
        public virtual GanjoorRelatedPerson Person2 { get; set; }

        /// <summary>
        /// the kind of tie between Person1 and Person2
        /// </summary>
        public PersonAffiliationType AffiliationType { get; set; }

        /// <summary>
        /// free-text note (e.g. sourcing/reasoning, or what the tie actually is when AffiliationType is Other)
        /// </summary>
        public string Note { get; set; }
    }
}
