using Microsoft.EntityFrameworkCore;
using RMuseum.DbContext;
using RMuseum.Models.Notification;
using RSecurityBackend.Models.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{

    /// <summary>
    /// Internal messaging system implementation
    /// </summary>
    public class RNotificationService : IRNotificationService
    {
        /// <summary>
        /// Add Notification
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="subject"></param>
        /// <param name="htmlText"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RUserNotification>> PushNotification(Guid userId, string subject, string htmlText)
        {
            try
            {
                RUserNotification notification =
                            new RUserNotification()
                            {
                                UserId = userId,
                                DateTime = DateTime.Now,
                                Status = NotificationStatus.Unread,
                                Subject = subject,
                                HtmlText = htmlText
                            };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                return new RServiceResult<RUserNotification>(notification);
            }
            catch (Exception exp)
            {
                return new RServiceResult<RUserNotification>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Switch Notification Status
        /// </summary>
        /// <param name="notificationId"></param>    
        /// <returns>updated notification object</returns>
        public async Task<RServiceResult<RUserNotification>> SwitchNotificationStatus(Guid notificationId)
        {
            try
            {
                RUserNotification notification =
                            await _context.Notifications.Where(n => n.Id == notificationId).SingleAsync();
                notification.Status = notification.Status == NotificationStatus.Unread ? NotificationStatus.Read : NotificationStatus.Unread;
                _context.Notifications.Update(notification);
                await _context.SaveChangesAsync();
                return new RServiceResult<RUserNotification>(notification);
            }
            catch (Exception exp)
            {
                return new RServiceResult<RUserNotification>(null, exp.ToString());
            }
        }


        /// <summary>
        /// Delete Notification
        /// </summary>
        /// <param name="notificationId"></param>    
        /// <returns></returns>
        public async Task<RServiceResult<bool>> DeleteNotification(Guid notificationId)
        {
            try
            {
                RUserNotification notification =
                            await _context.Notifications.Where(n => n.Id == notificationId).SingleAsync();
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
                return new RServiceResult<bool>(true);
            }
            catch (Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }
        }

        /// <summary>
        /// Get User Notifications
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<RUserNotification[]>> GetUserNotifications(Guid userId)
        {
            try
            {
                return new RServiceResult<RUserNotification[]>
                    (
                    await _context.Notifications
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.DateTime)
                    .ToArrayAsync()
                    );
            }
            catch (Exception exp)
            {
                return new RServiceResult<RUserNotification[]>(null, exp.ToString());
            }
        }

        /// <summary>
        /// Get Unread User Notifications Count
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<RServiceResult<int>> GetUnreadUserNotificationsCount(Guid userId)
        {
            try
            {
                return new RServiceResult<int>
                    (
                    await _context.Notifications
                    .Where(n => n.UserId == userId && n.Status == NotificationStatus.Unread)
                    .CountAsync()
                    );
            }
            catch (Exception exp)
            {
                return new RServiceResult<int>(0, exp.ToString());
            }
        }

        /// <summary>
        /// Database Contetxt
        /// </summary>
        protected readonly RMuseumDbContext _context;


        public RNotificationService(RMuseumDbContext context)
        {
            _context = context;
        }

    }
}
