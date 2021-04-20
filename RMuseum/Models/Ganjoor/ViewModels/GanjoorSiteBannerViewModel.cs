namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// Ganjoor Site Banner View Model
    /// </summary>
    public class GanjoorSiteBannerViewModel
    {
        /// <summary>
        /// id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// image url
        /// </summary>
        public string ImageUrl { get; set; }

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
