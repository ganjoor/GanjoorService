using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RSecurityBackend.Services;
using RSecurityBackend.Controllers;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;

namespace RMuseum.Controllers
{
    /// <summary>
    /// User login/logout/register/...
    /// </summary>
    [Produces("application/json")]
    [Route("api/users")]
    public class AppUserController : AppUserControllerBase
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="appUserService"></param>
        /// <param name="httpContextAccessor"></param>
        /// <param name="userPermissionChecker"></param>
        /// <param name="emailSender"></param>
        /// <param name="imageFileService"></param>
        /// <param name="captchaService"></param>
        public AppUserController(IConfiguration configuration, IAppUserService appUserService, IHttpContextAccessor httpContextAccessor, IUserPermissionChecker userPermissionChecker, IEmailSender emailSender, IImageFileService imageFileService, ICaptchaService captchaService)
            : base(configuration, appUserService, httpContextAccessor, userPermissionChecker, emailSender, imageFileService, captchaService)
        {
            
        }
    }
}
