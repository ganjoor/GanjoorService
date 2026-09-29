namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// a moderator's decision on a pending GanjoorPersonEditSuggestion
    /// </summary>
    public class PersonEditSuggestionModerationViewModel
    {
        /// <summary>
        /// review result
        /// </summary>
        public CorrectionReviewResult Result { get; set; }

        /// <summary>
        /// review note
        /// </summary>
        public string ReviewNote { get; set; }
    }
}
