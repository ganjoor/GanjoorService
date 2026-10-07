namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// what a GanjoorPersonRelationEditSuggestion proposes doing to the kinship graph
    /// </summary>
    public enum PersonRelationSuggestionAction
    {
        /// <summary>
        /// create a brand new GanjoorPersonRelation between Person1Id and Person2Id
        /// </summary>
        Add = 0,

        /// <summary>
        /// change the RelationType/DegreeHint/Note of the existing relation ExistingRelationId points to
        /// </summary>
        Modify = 1,

        /// <summary>
        /// remove the existing relation ExistingRelationId points to outright
        /// </summary>
        Remove = 2,

        /// <summary>
        /// attach one more piece of evidence (a poem couplet, see GanjoorPersonRelationEvidence) to the
        /// existing relation ExistingRelationId points to. Only valid for Kind == Family.
        /// </summary>
        AddEvidence = 3,

        /// <summary>
        /// detach the evidence row ExistingEvidenceId points to from its relation (the relation itself stays)
        /// </summary>
        RemoveEvidence = 4,
    }
}
