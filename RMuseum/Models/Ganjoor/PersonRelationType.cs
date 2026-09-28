namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// the kind of kinship edge a GanjoorPersonRelation represents between two GanjoorRelatedPerson
    /// rows. The graph is not a strict tree - siblings can be known without their parents, an
    /// ancestor can be known without the exact number of generations in between, etc. - so this is
    /// a small, deliberately open set rather than a rigid parent/child-only model.
    /// </summary>
    public enum PersonRelationType
    {
        /// <summary>
        /// Person1 is a parent of Person2 (directional - the mirror "child" relation is implied,
        /// not stored as a second row)
        /// </summary>
        Parent = 0,

        /// <summary>
        /// Person1 and Person2 are siblings (symmetric - order doesn't matter). Doesn't require
        /// either parent to be known/recorded.
        /// </summary>
        Sibling = 1,

        /// <summary>
        /// Person1 and Person2 are spouses (symmetric - order doesn't matter)
        /// </summary>
        Spouse = 2,

        /// <summary>
        /// Person1 is a known ancestor of Person2 (directional) but the exact number of generations
        /// between them is not known/recorded - see GanjoorPersonRelation.DegreeHint for the case
        /// where the exact degree (e.g. "grandparent") IS known
        /// </summary>
        Ancestor = 3,
    }
}
