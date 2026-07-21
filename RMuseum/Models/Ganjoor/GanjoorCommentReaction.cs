using RSecurityBackend.Models.Auth.Db;
using System;

namespace RMuseum.Models.Ganjoor
{
    public class GanjoorCommentReaction
    {
        public int Id { get; set; }

        public int GanjoorCommentId { get; set; }
        public virtual GanjoorComment GanjoorComment { get; set; }

        public Guid UserId { get; set; }
        public virtual RAppUser User { get; set; }

        /// <summary>
        /// +1 = Like
        /// -1 = Dislike
        /// </summary>
        public short Value { get; set; }

        public DateTime ReactionDate { get; set; }
    }
}
