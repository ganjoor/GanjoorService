namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// what a GanjoorPersonEditSuggestion asks for (deleting a person is the SuggestedForDeletion flag of an Edit suggestion)
    /// </summary>
    public enum PersonSuggestionOperation
    {
        /// <summary>
        /// edit the fields of the person (or delete it, see SuggestedForDeletion)
        /// </summary>
        Edit = 0,

        /// <summary>
        /// the person is a duplicate of MergeIntoPersonId
        /// </summary>
        Merge = 1,

        /// <summary>
        /// the person is really two people with the same name, see the Split* fields
        /// </summary>
        Split = 2,
    }
}
