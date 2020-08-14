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

    }
}
