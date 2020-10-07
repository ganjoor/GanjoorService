namespace RMuseum.Models.GanjoorAudio.ViewModels
{
    /// <summary>
    /// Poem Narration Moderation View Model
    /// </summary>
    public class PoemNarrationModerateViewModel
    {
        /// <summary>
        /// Approve or Reject
        /// </summary>
        public bool Approve { get; set; }

        /// <summary>
        /// Rejection Message
        /// </summary>
        public string Message { get; set; }
    }
}
