using RSecurityBackend.Models.Auth.Db;
using System;

namespace RMuseum.Models.GanjoorAudio
{
    /// <summary>
    /// recitation user up vote (the records would be deleted manually if the owner user account gets deleted)
    /// </summary>
    public class RecitationUserUpVote
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Recitation Id
        /// </summary>
        public int RecitationId { get; set; }

        /// <summary>
        /// Recitation
        /// </summary>
        public Recitation Recitation { get; set; }

        /// <summary>
        /// User Id, defining it this way causes no cascade relation
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// User
        /// </summary>
        public virtual RAppUser User { get; set; }

        /// <summary>
        /// date time
        /// </summary>
        public DateTime DateTime { get; set; }
    }
}
