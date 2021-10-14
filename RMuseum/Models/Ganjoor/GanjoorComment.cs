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
        public int PoemId { get; set; }

        /// <summary>
        /// Ganjoor Poem Id
        /// </summary>
        public GanjoorPoem Poem { get; set; }

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
        public string AuthorIpAddress { get; set; }

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
        public virtual GanjoorComment InReplyTo { get; set; }

        /// <summary>
        /// Reference Comment Id
        /// </summary>
        public int? InReplyToId { get; set; }

        /// <summary>
        /// publish status
        /// </summary>
        public PublishStatus Status { get; set; }

        /// <summary>
        /// related verse 1 Id, this is always null, because verses are removed and created on operations like editing and their Ids changed, so
        /// later I decided to rely on (PoemId + CoupletIndex) instead
        /// </summary>
        public int? Verse1Id { get; set; }

        /// <summary>
        /// do not use
        /// </summary>
        public virtual GanjoorVerse Verse1 { get; set; }

        /// <summary>
        /// related verse 2 Id, this is always null, because verses are removed and created on operations like editing and their Ids changed, so
        /// later I decided to rely on (PoemId + CoupletIndex) instead
        /// </summary>
        public int? Verse12d { get; set; }

        /// <summary>
        /// do not use
        /// </summary>
        public virtual GanjoorVerse Verse2 { get; set; }

        /// <summary>
        /// couplet index
        /// </summary>
        public int? CoupletIndex { get; set; }
    }
}
