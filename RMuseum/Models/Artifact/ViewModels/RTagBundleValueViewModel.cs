using System;

namespace RMuseum.Models.Artifact.ViewModels
{
    /// <summary>
    /// Tag Bundle View Model
    /// </summary>
    public class RTagBundleValueViewModel
    {
        /// <summary>
        /// Item Title
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Item Friendly Url
        /// </summary>
        public string FriendlyUrl { get; set; }

        /// <summary>
        /// Items Count
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Item Image Id
        /// </summary>
        public Guid ImageId { get; set; }

        /// <summary>
        /// url to access the image from THE external host, contains '/norm/' which when
        /// you replace it with '/thumb/' you would have ExternalThumbnailImageUrl
        /// and if you replace it with '/orig/' you would have a url for ExternalOriginalSizeImageUrl which MIGHT NOT EXIST and end in a 404 error
        /// </summary>
        public string ExternalNormalSizeImageUrl { get; set; }

    }
}
