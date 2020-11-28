using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RMuseum.Models.Notification;
using RMuseum.Services;
using RSecurityBackend.Models.Generic;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;


namespace RMuseum.Controllers
{
    [Produces("application/json")]
    [Route("api/notifications")]
    public class NoificationController : Controller
    {
        /// <summary>
        /// Get User Notifications
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RUserNotification[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetUserNotifications()
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<RUserNotification[]> res = await _notificationService.GetUserNotifications(loggedOnUserId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        [HttpGet]
        [Route("unread/count")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(int))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetUnreadUserNotificationsCount()
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<int> res = await _notificationService.GetUnreadUserNotificationsCount(loggedOnUserId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// Switch Notification Status
        /// </summary>
        /// <param name="notificationId"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("{notificationId}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RUserNotification))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> SwitchNotificationStatus(Guid notificationId)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<RUserNotification> res = await _notificationService.SwitchNotificationStatus(notificationId, loggedOnUserId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// Set All User Notifications Status Read
        /// </summary>
        /// <returns></returns>
        [HttpPut]
        [Route("allread")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> SetAllNotificationsStatusRead()
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<bool> res = await _notificationService.SetAllNotificationsStatus(loggedOnUserId, NotificationStatus.Read);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// Set All User Notifications Status Unread
        /// </summary>
        [HttpPut]
        [Route("allunread")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> SetAllNotificationsStatusUnread()
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<bool> res = await _notificationService.SetAllNotificationsStatus(loggedOnUserId, NotificationStatus.Unread);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// Delete Notification
        /// </summary>
        /// <param name="notificationId"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("{notificationId}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> DeleteNotification(Guid notificationId)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<bool> res = await _notificationService.DeleteNotification(notificationId, loggedOnUserId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok();
        }

        /// <summary>
        /// Notification Service
        /// </summary>
        protected readonly IRNotificationService _notificationService;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="notificationService"></param>
        public NoificationController(IRNotificationService notificationService)
        {
            _notificationService = notificationService;
        }
    }
}
