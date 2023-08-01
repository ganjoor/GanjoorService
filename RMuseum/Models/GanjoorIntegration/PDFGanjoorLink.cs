using RSecurityBackend.Models.Auth.Db;
using System;

namespace RMuseum.Models.GanjoorIntegration
{
    /// <summary>
    /// PDFBook Ganjoor Link
    /// </summary>
    public class PDFGanjoorLink
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
        /// pdf book id
        /// </summary>
        public int PDFBookId { get; set; }

        /// <summary>
        /// page number
        /// </summary>
        public int PageNumber { get; set; }

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
        /// is the is the text original source?
        /// </summary>
        public bool IsTextOriginalSource { get; set; }

        /// <summary>
        /// PDF Page Title
        /// </summary>
        public string PDFPageTitle { get; set; }

        /// <summary>
        /// external thumbnail image url
        /// </summary>
        public string ExternalThumbnailImageUrl { get; set; }
    }
}
