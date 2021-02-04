using System;

namespace RSecurityBackend.Models.Notification.ViewModels
{
    /// <summary>
    /// User Notification
    /// </summary>
    public class RUserNotificationViewModel
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }
        
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
