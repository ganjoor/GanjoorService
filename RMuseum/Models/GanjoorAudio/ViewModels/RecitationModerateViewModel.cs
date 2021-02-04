namespace RMuseum.Models.GanjoorAudio.ViewModels
{
    /// <summary>
    /// Poem Narration Moderation View Model
    /// </summary>
    public class RecitationModerateViewModel
    {
        /// <summary>
        /// Moderation Result
        /// </summary>
        public PoemNarrationModerationResult Result { get; set; }

        /// <summary>
        /// Rejection Message
        /// </summary>
        public string Message { get; set; }
    }
}
