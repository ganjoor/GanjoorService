using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RSecurityBackend.Services;
using RSecurityBackend.Controllers;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using System.Net;
using RSecurityBackend.Models.Auth.ViewModels;
using System.Threading.Tasks;
using Audit.WebApi;
using RSecurityBackend.Models.Generic;
using System;
using RMuseum.Models.Auth.ViewModel;

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
        /// login
        /// </summary>
        /// <param name="loginViewModel">loginViewModel</param>
        /// <returns>LoggedOnUserModel</returns>
        [HttpPost]
        [AllowAnonymous]
        [Route("login")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(LoggedOnUserModelEx))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public override async Task<IActionResult> Login(
            [AuditIgnore]
            [FromBody]
            LoginViewModel loginViewModel
            )
        {
            string clientIPAddress = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
            RServiceResult<LoggedOnUserModel> res = await _appUserService.Login(loginViewModel, clientIPAddress);
            if (res.Result == null)
            {
                return BadRequest(res.ExceptionString);
            }

            var l = res.Result;

            bool keepHistory = false;
            var kRes = await _optionsService.GetValueAsync("KeepHistory", l.User.Id);
            if (!string.IsNullOrEmpty(kRes.Result))
                bool.TryParse(kRes.Result, out keepHistory);

           
            LoggedOnUserModelEx loggedOnUserModelEx = new LoggedOnUserModelEx()
            {
                User = l.User,
                Token = l.Token,
                SessionId = l.SessionId,
                SecurableItem = l.SecurableItem,
                KeepHistory = keepHistory
            };


            return Ok(loggedOnUserModelEx);
        }

        /// <summary>
        /// renew an expired session
        /// </summary>
        /// <param name="sessionId">user session id</param>
        /// <returns>LoggedOnUserModel</returns>
        [HttpPut]
        [AllowAnonymous]
        [Route("relogin/{sessionId}")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(LoggedOnUserModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public override async Task<IActionResult> ReLogin(
            Guid sessionId
            )
        {
            string clientIPAddress = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
            RServiceResult<LoggedOnUserModel> res = await _appUserService.ReLogin(sessionId, clientIPAddress);
            if (res.Result == null)
            {
                return BadRequest(res.ExceptionString);
            }

            var l = res.Result;

            bool keepHistory = false;
            var kRes = await _optionsService.GetValueAsync("KeepHistory", l.User.Id);
            if (!string.IsNullOrEmpty(kRes.Result))
                bool.TryParse(kRes.Result, out keepHistory);


            LoggedOnUserModelEx loggedOnUserModelEx = new LoggedOnUserModelEx()
            {
                User = l.User,
                Token = l.Token,
                SessionId = l.SessionId,
                SecurableItem = l.SecurableItem,
                KeepHistory = keepHistory
            };

            return Ok(loggedOnUserModelEx);
        }

        /// <summary>
        /// options service
        /// </summary>

        protected readonly IRGenericOptionsService _optionsService;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="optionsService"></param>
        /// <param name="configuration"></param>
        /// <param name="appUserService"></param>
        /// <param name="httpContextAccessor"></param>
        /// <param name="userPermissionChecker"></param>
        /// <param name="emailSender"></param>
        /// <param name="imageFileService"></param>
        /// <param name="captchaService"></param>
        public AppUserController(
            IRGenericOptionsService optionsService,
            IConfiguration configuration, IAppUserService appUserService, IHttpContextAccessor httpContextAccessor, IUserPermissionChecker userPermissionChecker, IEmailSender emailSender, IImageFileService imageFileService, ICaptchaService captchaService)
            : base(configuration, appUserService, httpContextAccessor, userPermissionChecker, emailSender, imageFileService, captchaService)
        {
            _optionsService = optionsService;
        }
    }
}
