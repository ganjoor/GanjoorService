using RSecurityBackend.Models.Auth.Db;
using System;

namespace RSecurityBackend.Models.Notification
{
    /// <summary>
    /// User Notification
    /// </summary>
    public class RUserNotification
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// User Id
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// User
        /// </summary>
        public RAppUser User { get; set; }

        /// <summary>
        /// DateTime
        /// </summary>
        public DateTime DateTime { get; set; }

        /// <summary>
        /// Status
        /// </summary>
        public NotificationStatus Status { get; set; }

        /// <summary>
        /// Subject
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        /// Text
        /// </summary>
        public string HtmlText { get; set; }
    }
}
