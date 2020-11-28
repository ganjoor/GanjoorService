using RMuseum.Models.Notification;
using RSecurityBackend.Models.Generic;
using System;
using System.Threading.Tasks;

namespace RMuseum.Services
{
    /// <summary>
    /// Internal messaging system interface
    /// </summary>
    public interface IRNotificationService
    {
        /// <summary>
        /// Add Notification
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="subject"></param>
        /// <param name="htmlText"></param>
        /// <returns></returns>
        Task<RServiceResult<RUserNotification>> PushNotification(Guid userId, string subject, string htmlText);

        /// <summary>
        /// Get User Notifications
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<RUserNotification[]>> GetUserNotifications(Guid userId);

        /// <summary>
        /// Get Unread User Notifications Count
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<int>> GetUnreadUserNotificationsCount(Guid userId);

        /// <summary>
        /// Switch Notification Status
        /// </summary>
        /// <param name="notificationId"></param>
        /// <param name="userId"></param>    
        /// <returns>updated notification object</returns>
        Task<RServiceResult<RUserNotification>> SwitchNotificationStatus(Guid notificationId, Guid userId);

        /// <summary>
        /// Set All User Notifications Status
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        Task<RServiceResult<bool>> SetAllNotificationsStatus(Guid userId, NotificationStatus status);

        /// <summary>
        /// Delete Notification
        /// </summary>
        /// <param name="notificationId">if empty deletes all</param>
        /// <param name="userId"></param>    
        /// <returns></returns>
        Task<RServiceResult<bool>> DeleteNotification(Guid notificationId, Guid userId);
    }
}
