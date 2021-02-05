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
        /// replies
        /// </summary>

        public GanjoorCommentSummaryViewModel[] Replies { get; set; }

    }
 
}
