namespace RMuseum.Models.GanjoorAudio.ViewModels
{
    /// <summary>
    /// Moderation Result
    /// </summary>
    public enum PoemNarrationModerationResult
    {
        /// <summary>
        /// no moderation
        /// </summary>
        MetadataNeedsFixation = 0,
        /// <summary>
        /// approve 
        /// </summary>
        Approve = 1,
        /// <summary>
        /// reject
        /// </summary>
        Reject = 2
    }

    /// <summary>
    /// Poem Narration Moderation View Model
    /// </summary>
    public class PoemNarrationModerateViewModel
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
