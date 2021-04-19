using RSecurityBackend.Models.Image;
using System;

namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// ganjoor.net site banner
    /// </summary>
    public class GanjoorSiteBanner
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// banner image
        /// </summary>
        public RImage RImage { get; set; }

        /// <summary>
        /// image id
        /// </summary>
        public Guid RImageId { get; set; }

        /// <summary>
        /// alternate text
        /// </summary>
        public string AlternateText { get; set; }

        /// <summary>
        /// target url
        /// </summary>
        public string TargetUrl { get; set; }

        /// <summary>
        /// active
        /// </summary>
        public bool Active { get; set; }
    }
}
