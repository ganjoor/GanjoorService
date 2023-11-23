using Microsoft.AspNetCore.Mvc;
using RSecurityBackend.Controllers;
using RSecurityBackend.Services;

namespace RMuseum.Controllers
{
    /// <summary>
    /// Notifications controller
    /// </summary>
    [Produces("application/json")]
    [Route("api/notifications")]
    public class NoificationController : NotificationControllerBase
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="notificationService"></param>
        public NoificationController(IRNotificationService notificationService) : base(notificationService)
        {
        }
    }
}
