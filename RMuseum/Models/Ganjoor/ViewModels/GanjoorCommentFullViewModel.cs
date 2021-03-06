﻿using System;

namespace RMuseum.Models.Ganjoor.ViewModels
{
    public class GanjoorCommentFullViewModel
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
        /// User Id
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// in reply to
        /// </summary>

        public GanjoorCommentSummaryViewModel InReplayTo { get; set; }

        /// <summary>
        /// poem
        /// </summary>
        public GanjoorPoemSummaryViewModel Poem { get; set; }


        /// <summary>
        /// this can be used by clients
        /// </summary>
        public bool MyComment { get; set; }
    }
}
