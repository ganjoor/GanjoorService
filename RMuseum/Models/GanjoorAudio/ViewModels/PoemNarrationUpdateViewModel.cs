namespace RMuseum.Models.GanjoorAudio.ViewModels
{
    /// <summary>
    /// Poem Narration View Model for updates
    /// </summary>
    public class PoemNarrationUpdateViewModel
    {
        /// <summary>
        /// Audio Title
        /// </summary>
        public string AudioTitle { get; set; }

        /// <summary>
        /// Audio Artist
        /// </summary>
        public string AudioArtist { get; set; }

        /// <summary>
        /// Audio Artist Url
        /// </summary>
        public string AudioArtistUrl { get; set; }

        /// <summary>
        /// Audio Source
        /// </summary>
        public string AudioSrc { get; set; }

        /// <summary>
        /// Audio Src Url
        /// </summary>
        public string AudioSrcUrl { get; set; }

        /// <summary>
        /// Review Status
        /// </summary>
        /// <remarks>
        /// changing from Draft => Pending or Pending => Draft is possible for owner, other changes need narration::moderate permission
        /// </remarks>
        public AudioReviewStatus ReviewStatus { get; set; }
    }
}
