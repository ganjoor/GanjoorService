using System;

namespace RMuseum.Models.GanjoorIntegration.ViewModels
{
    /// <summary>
    /// pinterest / ganjoor link suggestion view model
    /// </summary>
    public class PinterestSuggestion
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
        /// alt text
        /// </summary>
        public string AltText { get; set; }

        /// <summary>
        /// link type
        /// </summary>
        public LinkType LinkType { get; set; }

        /// <summary>
        /// pinterest url
        /// </summary>
        public string PinterestUrl { get; set; }

        /// <summary>
        /// pinterest image url
        /// </summary>
        public string PinterestImageUrl { get; set; }
    }
}
