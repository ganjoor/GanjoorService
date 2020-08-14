using System;

namespace RMuseum.Models.GanjoorIntegration.ViewModels
{
    /// <summary>
    /// Link Suggestion
    /// </summary>
    public class LinkSuggestion
    {
        /// <summary>
        /// Ganjoor Post Id
        /// </summary>
        public int GanjoorPostId { get; set; }

        /// <summary>
        /// ganjoor url
        /// </summary>
        public string GanjoorUrl { get; set; }

        /// <summary>
        /// ganjoor title
        /// </summary>
        public string GanjoorTitle { get; set; }

        /// <summary>
        /// Artifact Friendly Url
        /// </summary>
        public string ArtifactFriendlyUrl { get; set; }

        /// <summary>
        /// Artifact Item Id
        /// </summary>
        public Guid? ItemId { get; set; }
    }
}
