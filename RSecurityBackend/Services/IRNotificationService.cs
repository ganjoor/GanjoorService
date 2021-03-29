using RSecurityBackend.Models.Notification;
using RSecurityBackend.Models.Notification.ViewModels;
using RSecurityBackend.Models.Generic;
using System;
using System.Threading.Tasks;

namespace RSecurityBackend.Services
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
        Task<RServiceResult<RUserNotificationViewModel>> PushNotification(Guid userId, string subject, string htmlText);

        /// <summary>
        /// Get User Notifications
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<RUserNotificationViewModel[]>> GetUserNotifications(Guid userId);

        /// <summary>
        /// Get User Notifications (paginated version)
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<RServiceResult<(PaginationMetadata PagingMeta, RUserNotificationViewModel[] Items)>> GetUserNotificationsPaginated(PagingParameterModel paging, Guid userId);

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
        Task<RServiceResult<RUserNotificationViewModel>> SwitchNotificationStatus(Guid notificationId, Guid userId);

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
        /// <param name="notificationId">if empty deletes all read notifications</param>
        /// <param name="userId"></param>    
        /// <returns></returns>
        Task<RServiceResult<bool>> DeleteNotification(Guid notificationId, Guid userId);
    }
}
