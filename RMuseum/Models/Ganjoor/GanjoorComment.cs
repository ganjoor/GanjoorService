using RMuseum.Models.Artifact;
using RSecurityBackend.Models.Auth.Db;
using System;

namespace RMuseum.Models.Ganjoor
{
    /// <summary>
    /// Ganjoor Comment
    /// </summary>
    public class GanjoorComment
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Ganjoor Poem Id
        /// </summary>
        public int GanjoorPoemId { get; set; }

        /// <summary>
        /// Ganjoor Poem Id
        /// </summary>
        public GanjoorPoem GanjoorPoem { get; set; }

        /// <summary>
        /// user id
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// user
        /// </summary>
        public virtual RAppUser User { get; set; }

        /// <summary>
        /// author name (MySql imported field)
        /// </summary>
        public string AuthorName { get; set; }

        /// <summary>
        /// author email (MySql imported field)
        /// </summary>
        public string AuthorEmail { get; set; }

        /// <summary>
        /// author url (MySql imported field)
        /// </summary>
        public string AuthorUrl { get; set; }

        /// <summary>
        /// author IP address (MySql imported field)
        /// </summary>
        public string AutherIpAddress { get; set; }

        /// <summary>
        /// comment date
        /// </summary>
        public DateTime CommentDate { get; set; }

        /// <summary>
        /// comment
        /// </summary>
        public string HtmlComment { get; set; }

        /// <summary>
        /// In Reply to Other Comment
        /// </summary>
        public virtual GanjoorComment ReferenceComment { get; set; }

        /// <summary>
        /// Reference Comment Id
        /// </summary>
        public Guid? ReferenceCommentId { get; set; }

        /// <summary>
        /// publish status
        /// </summary>
        public PublishStatus Status { get; set; }
    }
}
