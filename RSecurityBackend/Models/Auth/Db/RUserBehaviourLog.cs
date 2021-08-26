using System;

namespace RSecurityBackend.Models.Auth.Db
{
    /// <summary>
    /// logs for user bad actions which may due to rule end in him or her being banned from the platform
    /// </summary>
    public class RUserBehaviourLog
    {
        /// <summary>
        /// id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// user id
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// user
        /// </summary>
        public RAppUser User { get; set; }

        /// <summary>
        /// datetime
        /// </summary>
        public DateTime DateTime { get; set; }

        /// <summary>
        /// description
        /// </summary>
        public string Description { get; set; }
    }
}
