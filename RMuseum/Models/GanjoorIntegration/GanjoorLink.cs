using RMuseum.Models.Artifact;
using RSecurityBackend.Models.Auth.Db;
using System;

namespace RMuseum.Models.GanjoorIntegration
{
    /// <summary>
    /// ganjoor link for artifacts and items
    /// </summary>
    public class GanjoorLink
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

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
        /// Artifact Id
        /// </summary>
        public Guid ArtifactId { get; set; }

        /// <summary>
        /// artifact
        /// </summary>
        public RArtifactMasterRecord Artifact { get; set; }

        /// <summary>
        /// Artifact Item Id
        /// </summary>
        public Guid? ItemId { get; set; }

        /// <summary>
        /// Artifact Item
        /// </summary>
        public virtual RArtifactItemRecord Item { get; set; }

        /// <summary>
        /// User Id who suggested the link
        /// </summary>
        public Guid SuggestedById { get; set; }

        /// <summary>
        /// Suggestion Date
        /// </summary>
        public DateTime SuggestionDate { get; set; }

        /// <summary>
        /// User who suggested the link
        /// </summary>
        public RAppUser SuggestedBy { get; set; }

        /// <summary>
        /// User id who reviewed the link
        /// </summary>
        public Guid? ReviewerId { get; set; }

        /// <summary>
        /// User who reviewed the link
        /// </summary>
        public virtual RAppUser Reviewer { get; set; }

        /// <summary>
        /// Review Date
        /// </summary>
        public DateTime ReviewDate { get; set; }

        /// <summary>
        /// review result
        /// </summary>
        public ReviewResult ReviewResult { get; set; }

        /// <summary>
        /// Synchronized with ganjoor
        /// </summary>
        public bool Synchronized { get; set; }

        /// <summary>
        /// display this image on poem page
        /// </summary>
        public bool DisplayOnPage { get; set; }

        /// <summary>
        /// original source url
        /// </summary>
        public string OriginalSourceUrl { get; set; }

        /// <summary>
        /// link to original source
        /// </summary>
        public bool LinkToOriginalSource { get; set; }

        /// <summary>
        /// is text original source
        /// </summary>
        public bool IsTextOriginalSource { get; set; }
    }
}
