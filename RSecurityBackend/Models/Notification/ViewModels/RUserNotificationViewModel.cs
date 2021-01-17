using System;

namespace RSecurityBackend.Models.Notification.ViewModels
{
    /// <summary>
    /// User Notification
    /// </summary>
    public class RUserNotificationViewModel
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="src"></param>
        public RUserNotificationViewModel(RUserNotification src)
        {
            Id = src.Id;
            DateTime = src.DateTime;
            Status = src.Status;
            Subject = src.Subject;
            HtmlText = src.HtmlText;
        }
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
