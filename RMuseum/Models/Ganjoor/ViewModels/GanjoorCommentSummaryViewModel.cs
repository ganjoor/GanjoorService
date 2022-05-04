using System;

namespace RMuseum.Models.Ganjoor.ViewModels
{
    /// <summary>
    /// GanjoorComment Summary View Model
    /// </summary>
    public class GanjoorCommentSummaryViewModel
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// author name (MySql imported field)
        /// </summary>
        public string AuthorName { get; set; }

        /// <summary>
        /// author url (MySql imported field)
        /// </summary>
        public string AuthorUrl { get; set; }

        /// <summary>
        /// comment date
        /// </summary>
        public DateTime CommentDate { get; set; }

        /// <summary>
        /// comment
        /// </summary>
        public string HtmlComment { get; set; }

        /// <summary>
        /// status
        /// </summary>
        public string PublishStatus { get; set; }


        /// <summary>
        /// in reply to
        /// </summary>
        public int? InReplyToId { get; set; }

        /// <summary>
        /// User Id
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// replies
        /// </summary>

        public GanjoorCommentSummaryViewModel[] Replies { get; set; }

        /// <summary>
        /// this can be used by clients
        /// </summary>
        public bool MyComment { get; set; }

        /// <summary>
        /// couplet index
        /// </summary>
        public int CoupletIndex { get; set; }

        /// <summary>
        /// couplet summary
        /// </summary>
        public string CoupletSummary { get; set; }

        /// <summary>
        /// for client
        /// </summary>
        public bool IsBookmarked { get; set; }

    }
 
}
