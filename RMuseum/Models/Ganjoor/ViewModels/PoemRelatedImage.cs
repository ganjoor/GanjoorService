namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// Related Image Type
    /// </summary>
    public enum PoemRelatedImageType
    {
        /// <summary>
        /// musueum.ganjoor.net link
        /// </summary>
        MuseumLink = 0,
        /// <summary>
        /// external link
        /// </summary>
        ExternalLink = 1
    }

    /// <summary>
    /// Poem Related Images
    /// </summary>
    public class PoemRelatedImage
    {
        /// <summary>
        /// Image Order
        /// </summary>
        public int ImageOrder { get; set; }
        /// <summary>
        /// poem related image type
        /// </summary>
        public PoemRelatedImageType PoemRelatedImageType { get; set; }

        /// <summary>
        /// thumbnail image url
        /// </summary>
        public string ThumbnailImageUrl { get; set; }

        /// <summary>
        /// target page url
        /// </summary>
        public string TargetPageUrl { get; set; }

        /// <summary>
        /// alternate text
        /// </summary>
        public string AltText { get; set; }

        /// <summary>
        /// is text original source
        /// </summary>
        public bool IsTextOriginalSource { get; set; }
    }
}
