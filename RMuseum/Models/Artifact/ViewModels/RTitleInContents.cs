namespace RMuseum.Models.Artifact.ViewModels
{
    /// <summary>
    /// Title of an Item in Artifact Contents
    /// </summary>
    public class RTitleInContents
    {
        /// <summary>
        /// Title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Order
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Level
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Item Friendly Url
        /// </summary>
        public string ItemFriendlyUrl { get; set; }
    }
}
