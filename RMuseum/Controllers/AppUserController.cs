using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RSecurityBackend.Services;
using RSecurityBackend.Controllers;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using System.Net;
using System.Threading.Tasks;
using System;
using System.Linq;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Models.Auth.ViewModels;
using RSecurityBackend.Models.Auth.Db;

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
        
        /// <summary>
        /// start user self delete process (send a verification email to user)
        /// </summary>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        [HttpPost("selfdelete/start")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public virtual async Task<IActionResult> StartLeaving(SelfDeleteViewModel viewModel)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            Guid sessionId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value);
            string clientIPAddress = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
            var verifyRes = (await _appUserService.StartLeaving(loggedOnUserId, sessionId, clientIPAddress)).Result;
            var user = (await _appUserService.GetUserInformation(loggedOnUserId)).Result;

            try
            {
                await _emailSender.SendEmailAsync
                    (
                    user.Email,
                    _appUserService.GetEmailSubject(RVerifyQueueType.UserSelfDelete, verifyRes.Secret),
                    _appUserService.GetEmailHtmlContent(RVerifyQueueType.UserSelfDelete, verifyRes.Secret, viewModel.CallbackUrl)
                    );
                return Ok();
            }
            catch (Exception exp)
            {
                return BadRequest("Error sending email: " + exp.ToString());
            }
        }
    }
}
