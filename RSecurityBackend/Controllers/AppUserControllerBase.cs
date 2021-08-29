using Audit.WebApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RSecurityBackend.Models.Auth.Db;
using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Models.Auth.ViewModels;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Models.Image;
using RSecurityBackend.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace RSecurityBackend.Controllers
{
    /// <summary>
    /// User login/logout/register/...
    /// </summary>
    [Produces("application/json")]
    [Route("api/users")]
    public abstract class AppUserControllerBase : Controller
    {
        /// <summary>
        /// login
        /// </summary>
        /// <param name="loginViewModel">loginViewModel</param>
        /// <returns>LoggedOnUserModel</returns>
        [HttpPost]
        [AllowAnonymous]
        [Route("login")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(LoggedOnUserModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> Login(
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

            return Ok(res.Result);
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
        public async Task<IActionResult> ReLogin(
            Guid sessionId
            )
        {
            string clientIPAddress = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
            RServiceResult<LoggedOnUserModel> res = await _appUserService.ReLogin(sessionId, clientIPAddress);
            if (res.Result == null)
            {
                return BadRequest(res.ExceptionString);
            }

            return Ok(res.Result);
        }

        /// <summary>
        /// Logout user (users need user:delothersession to logout other users)
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="sessionId">User Session Id</param>
        /// <returns></returns>
        [HttpDelete]
        [Authorize]
        [Route("delsession")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> Logout(
            Guid userId,
            Guid sessionId
            )
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            if (loggedOnUserId != userId)
            {
                RServiceResult<bool> canLogoutAllUsers =
                    await _userPermissionChecker.Check
                    (
                        loggedOnUserId,
                        new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value),
                        SecurableItem.UserEntityShortName,
                        SecurableItem.DelOtherUserSessionOperationShortName
                        );

                if (!string.IsNullOrEmpty(canLogoutAllUsers.ExceptionString))
                    return BadRequest(canLogoutAllUsers.ExceptionString);
                if (!canLogoutAllUsers.Result)
                    return Forbid();
            }

            RServiceResult<bool> res = await _appUserService.Logout(userId, sessionId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
            {
                return BadRequest(res.ExceptionString);
            }

            return Ok(res.Result);
        }


        /// <summary>
        /// Check if my session is valid
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        [Route("checkmysession")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> IsSessionValid(Guid sessionId)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

            RServiceResult<bool> res = await _appUserService.SessionExists(loggedOnUserId, sessionId);

            if (!string.IsNullOrEmpty(res.ExceptionString))
            {
                return BadRequest(res.ExceptionString);
            }
            return Ok(res.Result);
        }

        /// <summary>
        /// get logged on user securableitems (permissions)
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        [Route("securableitems")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<SecurableItem>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetUserSecurableItemsStatus()
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

            RServiceResult<SecurableItem[]> res = await _appUserService.GetUserSecurableItemsStatus(loggedOnUserId);

            if (!string.IsNullOrEmpty(res.ExceptionString))
            {
                return BadRequest(res.ExceptionString);
            }
            return Ok(res.Result);
        }



        /// <summary>
        /// Paginated Users Information (if user does not have user:view permission list only contains him/her information)
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="filterByEmail"></param>
        /// <returns>All Users Information</returns>
        [HttpGet]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<PublicRAppUser>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> Get([FromQuery] PagingParameterModel paging, string filterByEmail = null)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

            RServiceResult<bool> canViewAllUsersInformation =
                await _userPermissionChecker.Check
                (
                    loggedOnUserId,
                    new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value),
                    SecurableItem.UserEntityShortName,
                    SecurableItem.ViewAllOperationShortName
                    );
            if (!string.IsNullOrEmpty(canViewAllUsersInformation.ExceptionString))
                return BadRequest(canViewAllUsersInformation.ExceptionString);
            if (canViewAllUsersInformation.Result)
            {
                if (!string.IsNullOrEmpty(filterByEmail))
                    filterByEmail = filterByEmail.Trim();
                RServiceResult<(PaginationMetadata PagingMeta, PublicRAppUser[] Items)> usersInfo
                    = await _appUserService.GetAllUsersInformation(paging, filterByEmail);
                if (!string.IsNullOrEmpty(usersInfo.ExceptionString))
                {
                    return BadRequest(usersInfo.ExceptionString);
                }
                // Paging Header
                HttpContext.Response.Headers.Add("paging-headers", JsonConvert.SerializeObject(usersInfo.Result.PagingMeta));

                return Ok(usersInfo.Result.Items);
            }
            else
            {
                RServiceResult<PublicRAppUser> userInfo = await _appUserService.GetUserInformation(loggedOnUserId);
                if (userInfo.Result == null)
                {
                    if (string.IsNullOrEmpty(userInfo.ExceptionString))
                        return NotFound();
                    return BadRequest(userInfo.ExceptionString);
                }
                HttpContext.Response.Headers.Add("paging-headers", JsonConvert.SerializeObject(new PaginationMetadata()
                {
                    currentPage = 1,
                    hasNextPage = false,
                    hasPreviousPage = false,
                    pageSize = 1,
                    totalCount = 1,
                    totalPages = 1
                }));

                return Ok(new PublicRAppUser[] { userInfo.Result });
            }
        }

        /// <summary>
        /// returns user information (if user does not have user:view permission trying to view other users' information fails with a forbidden error)
        /// </summary>
        /// <param name="id">user id</param>
        /// <returns>user information</returns>
        [HttpGet("{id}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(PublicRAppUser))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> Get(Guid id)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

            if (loggedOnUserId != id)
            {
                RServiceResult<bool> canViewAllUsersInformation =
                    await _userPermissionChecker.Check
                    (
                        loggedOnUserId,
                        new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value),
                        SecurableItem.UserEntityShortName,
                        SecurableItem.ViewAllOperationShortName
                        );
                if (!string.IsNullOrEmpty(canViewAllUsersInformation.ExceptionString))
                    return BadRequest(canViewAllUsersInformation.ExceptionString);

                if (!canViewAllUsersInformation.Result)
                    return Forbid();
            }


            RServiceResult<PublicRAppUser> userInfo = await _appUserService.GetUserInformation(id);
            if (userInfo.Result == null)
            {
                if (string.IsNullOrEmpty(userInfo.ExceptionString))
                    return NotFound();
                return BadRequest(userInfo.ExceptionString);
            }
            return Ok(userInfo.Result);
        }

        /// <summary>
        /// add a new user (if you are trying to add an admin user you yourself should be admin)
        /// </summary>
        /// <param name="newUserInfo">if passsword is sent empty system genrates one for it which could be retrieved from returned record</param>
        /// <returns>id/generated password if required could be retrieved from return value</returns>
        [HttpPost]
        [Authorize(Policy = SecurableItem.UserEntityShortName + ":" + SecurableItem.AddOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RegisterRAppUser))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public virtual async Task<IActionResult> Post([FromBody] RegisterRAppUser newUserInfo)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            if (newUserInfo.IsAdmin)
            {
                RServiceResult<bool> isAdmin = await _appUserService.IsAdmin(loggedOnUserId);
                if (!string.IsNullOrEmpty(isAdmin.ExceptionString))
                    return BadRequest(isAdmin.ExceptionString);
                if (!isAdmin.Result)
                    return Forbid();//Only admin users can create admin users
            }
            RServiceResult<RAppUser> result = await _appUserService.AddUser(newUserInfo);
            if (result.Result == null)
                return BadRequest(result.ExceptionString);
            RegisterRAppUser registerRAppUser = new RegisterRAppUser()
            {
                Email = result.Result.Email,
                Status = result.Result.Status,
                FirstName = result.Result.FirstName,
                SureName = result.Result.SureName,
                Id = result.Result.Id,
                IsAdmin = newUserInfo.IsAdmin,
                PhoneNumber = newUserInfo.PhoneNumber,
                RImageId = newUserInfo.RImageId,
                Username = newUserInfo.Username,
                NickName = newUserInfo.NickName,
                Bio = newUserInfo.Bio,
                Website = newUserInfo.Website
            };
            return Ok(registerRAppUser);
        }

        /// <summary>
        /// update existing user (if you are trying to update an admin user you yourself should be admin) (if user does not have user:modify permission trying to modify other users' information fails with a forbidden error)
        /// </summary>
        /// <param name="id">user id</param>
        /// <param name="existingUserInfo">existingUserInfo.id could be passed empty and it is ignored completely, if password is sent empty it does not has effect</param>
        /// <returns>true if succeeds</returns>
        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public virtual async Task<IActionResult> Put(Guid id, [FromBody] RegisterRAppUser existingUserInfo)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<bool> isAdmin = await _appUserService.IsAdmin(loggedOnUserId);
            if (!string.IsNullOrEmpty(isAdmin.ExceptionString))
                return BadRequest(isAdmin.ExceptionString);
            RServiceResult<PublicRAppUser> userInfo = await _appUserService.GetUserInformation(id);
            if (!isAdmin.Result)
            {

                if (!string.IsNullOrEmpty(userInfo.ExceptionString))
                    return BadRequest(userInfo.ExceptionString);

                if (existingUserInfo.IsAdmin)
                    return Forbid();//You should be admin to make other users admin
                RServiceResult<bool> isEditingUserAdmin = await _appUserService.IsAdmin(id);
                if (!string.IsNullOrEmpty(isEditingUserAdmin.ExceptionString))
                    return BadRequest(isEditingUserAdmin.ExceptionString);
                if (isEditingUserAdmin.Result)
                    return Forbid();//You can not modify admin users.

                if (loggedOnUserId != id)
                {
                    RServiceResult<bool> canViewAllUsersInformation =
                        await _userPermissionChecker.Check
                        (
                            loggedOnUserId,
                            new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value),
                            SecurableItem.UserEntityShortName,
                            SecurableItem.ModifyOperationShortName
                            );
                    if (!string.IsNullOrEmpty(canViewAllUsersInformation.ExceptionString))
                        return BadRequest(canViewAllUsersInformation.ExceptionString);

                    if (!canViewAllUsersInformation.Result)
                        return Forbid();
                }
            }

            if (loggedOnUserId == id && userInfo.Result.Username != existingUserInfo.Username)
                return BadRequest("You can not change your username!");
            if (loggedOnUserId == id && (existingUserInfo.Status != RAppUserStatus.Active))
                return BadRequest("You can not disable yourself!");
            if (loggedOnUserId == id && !string.IsNullOrEmpty(existingUserInfo.Password))
                return BadRequest("Please use setmypassword method to change your own password.");

            RServiceResult<bool> res = await _appUserService.ModifyUser(id, existingUserInfo);
            if (!res.Result)
                return BadRequest(res.ExceptionString);

            return Ok(true);

        }

        /// <summary>
        /// set my password
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        [Route("setmypassword")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public virtual async Task<IActionResult> SetMyPassword([AuditIgnore][FromBody] SetPasswordModel model)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);


            RServiceResult<bool> res = await _appUserService.ChangePassword(loggedOnUserId, model.OldPassword, model.NewPassword);
            if (!res.Result)
                return BadRequest(res.ExceptionString);

            return Ok(true);

        }

        /// <summary>
        /// delete user (only admin users can delete other admin users, a user cannot delete himself/herself)
        /// </summary>
        /// <param name="id">user id</param>
        /// <returns>true if succeeds</returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = SecurableItem.UserEntityShortName + ":" + SecurableItem.DeleteOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public virtual async Task<IActionResult> Delete(Guid id)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<bool> isAdmin = await _appUserService.IsAdmin(loggedOnUserId);
            if (!string.IsNullOrEmpty(isAdmin.ExceptionString))
                return BadRequest(isAdmin.ExceptionString);
            if (!isAdmin.Result)
            {
                RServiceResult<bool> isDeletingUserAdmin = await _appUserService.IsAdmin(id);
                if (!string.IsNullOrEmpty(isDeletingUserAdmin.ExceptionString))
                    return BadRequest(isDeletingUserAdmin.ExceptionString);
                if (isDeletingUserAdmin.Result)
                    return Forbid();//You can not delete admin users.
            }

            if (loggedOnUserId == id)
            {
                return BadRequest("SOS! Suicide attempt detected!");
            }

            RServiceResult<bool> res = await _appUserService.DeleteUser(id);
            if (!res.Result)
            {
                return BadRequest(res.ExceptionString);
            }

            return Ok(true);
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
        public async Task<IActionResult> StartLeaving(
            [AuditIgnore]
            [FromBody]
            SelfDeleteViewModel viewModel)
        {
            if (SiteInReadOnlyMode)
                return BadRequest("سایت به دلایل فنی مثل انتقال سرور موقتاً در حالت فقط خواندنی قرار دارد. لطفاً ساعاتی دیگر مجدداً برای حذف حساب کاربری تلاش کنید.");

            if (!SignupEnabled)
                return BadRequest("ثبت نام و حذف کاربر غیرفعال است.");

            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            Guid sessionId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value);
            string clientIPAddress = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();

            var user = (await _appUserService.GetUserInformation(loggedOnUserId)).Result;
            var session = await _appUserService.GetUserSession(loggedOnUserId, sessionId);

            RServiceResult<bool> resCheckAdmin = await _appUserService.IsAdmin(loggedOnUserId);
            if(resCheckAdmin.Result)
                return BadRequest("شما کاربر مدیر سیستم هستید. لطفا ابتدا مدیر سیستم دیگری ایجاد کنید یا از مدیر دیگری بخواهید شما را از حالت مدیر سیستمی خارج کند.");

            var loginResult = await _appUserService.Login(new LoginViewModel()
            {
                Username = user.Email,
                Password = viewModel.Password,
                ClientAppName = session.Result.ClientAppName,
                Language = session.Result.Language
            },
            clientIPAddress);

            if (loginResult.Result == null)
            {
                return BadRequest(loginResult.ExceptionString);
            }

            var verifyRes = (await _appUserService.StartLeaving(loggedOnUserId, sessionId, clientIPAddress)).Result;

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

        /// <summary>
        /// finalize user self delete
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        [HttpDelete("selfdelete/finalize/{code}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> FinializeLeaving(string code)
        {
            if (SiteInReadOnlyMode)
                return BadRequest("سایت به دلایل فنی مثل انتقال سرور موقتاً در حالت فقط خواندنی قرار دارد. لطفاً ساعاتی دیگر مجدداً برای حذف حساب کاربری تلاش کنید.");

            if (!SignupEnabled)
                return BadRequest("ثبت نام و حذف کاربر غیرفعال است.");


            RServiceResult<string> res = await _appUserService.RetrieveEmailFromQueueSecret(RVerifyQueueType.UserSelfDelete, code);

            if (!string.IsNullOrEmpty(res.ExceptionString))
            {
                return BadRequest(res.ExceptionString);
            }
            if (string.IsNullOrWhiteSpace(res.Result))
            {
                return NotFound("رمز وارد شده تطابق ندارد");
            }
            //override _appUserService.RemoveUserData to remove userdata

            RServiceResult<bool> resDelete = await _appUserService.DeleteUser(new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value));
            if (!resDelete.Result)
            {
                return BadRequest(res.ExceptionString);
            }

            return Ok();
        }

        /// <summary>
        /// Checks if user is admin
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        [Route("isadmin")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> IsAdmin(Guid userId)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<bool> isAdmin = await _appUserService.IsAdmin(loggedOnUserId);
            if (!string.IsNullOrEmpty(isAdmin.ExceptionString))
                return BadRequest(isAdmin.ExceptionString);
            if (!isAdmin.Result)
            {
                if (userId != loggedOnUserId)
                {
                    return Forbid();
                }
                return Ok(false);
            }

            if (userId == loggedOnUserId)
            {
                return Ok(true);
            }

            RServiceResult<bool> res = await _appUserService.IsAdmin(userId);

            if (!string.IsNullOrEmpty(res.ExceptionString))
            {
                return BadRequest(res.ExceptionString);
            }
            return Ok(res.Result);
        }

        /// <summary>
        /// View User Sessions (user needs user:sessions permission to view other users sessions)
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("sessions")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<PublicRUserSession>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> GetUserSessions(Guid? userId)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            if (loggedOnUserId != userId)
            {
                RServiceResult<bool> canViewAllUsersInformation =
                    await _userPermissionChecker.Check
                    (
                        loggedOnUserId,
                        new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value),
                        SecurableItem.UserEntityShortName,
                        SecurableItem.SessionsOperationShortName
                        );
                if (!string.IsNullOrEmpty(canViewAllUsersInformation.ExceptionString))
                    return BadRequest(canViewAllUsersInformation.ExceptionString);

                if (!canViewAllUsersInformation.Result)
                    return Forbid();
            }
            RServiceResult<PublicRUserSession[]> sessionsInfo = await _appUserService.GetUserSessions(userId);
            if (sessionsInfo.Result == null)
            {
                return BadRequest(sessionsInfo.ExceptionString);
            }
            return Ok(sessionsInfo.Result);
        }


        /// <summary>
        /// Set User Image (via FormData, specifying userId as 'id' in formData ) - if Files.count is 0 image would be removed - (users need user:modify to change other users image)
        /// </summary>
        /// <returns>new image id</returns>
        [HttpPost]
        [Route("image")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden, Type = typeof(string))]
        public async Task<IActionResult> SetUserImage()
        {
            try
            {
                if (!Request.Form.TryGetValue("id", out Microsoft.Extensions.Primitives.StringValues tmp))
                {
                    return BadRequest("id is null");
                }

                Guid userId = new Guid(tmp);

                Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

                if (loggedOnUserId != userId)
                {
                    RServiceResult<bool> canViewAllUsersInformation =
                        await _userPermissionChecker.Check
                        (
                            loggedOnUserId,
                            new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value),
                            SecurableItem.UserEntityShortName,
                            SecurableItem.ModifyOperationShortName
                            );
                    if (!string.IsNullOrEmpty(canViewAllUsersInformation.ExceptionString))
                        return BadRequest(canViewAllUsersInformation.ExceptionString);

                    if (!canViewAllUsersInformation.Result)
                        return Forbid();
                }

                RServiceResult<Guid?> res = await _appUserService.SetUserImage(userId, Request.Form.Files);

                if (!string.IsNullOrEmpty(res.ExceptionString))
                {
                    return BadRequest(res.ExceptionString);
                }

                if (res.Result == null)
                {
                    return Ok("");
                }

                return Ok(res.Result.ToString());
            }
            catch (Exception exp)
            {
                return BadRequest(exp.ToString());
            }
        }

        /// <summary>
        /// Get User Image in base 64
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]//no specific permission
        [Route("base64image")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetUserImageBase64(Guid id)
        {
            try
            {
                RServiceResult<RImage> img = await _appUserService.GetUserImage(id);

                if (!string.IsNullOrEmpty(img.ExceptionString))
                {
                    return BadRequest(img.ExceptionString);

                }
                if (img.Result == null)
                    return new ObjectResult(string.Empty);

                RServiceResult<string> imgPath = _imageFileService.GetImagePath(img.Result);
                if (!string.IsNullOrEmpty(imgPath.ExceptionString))
                    return BadRequest(imgPath.ExceptionString);


                return new ObjectResult(Convert.ToBase64String(System.IO.File.ReadAllBytes(imgPath.Result)));

            }
            catch (Exception exp)
            {
                return BadRequest(exp.ToString());
            }
        }

        /// <summary>
        /// Get User Image in base 64
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]//no specific permission
        [Route("image")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(FileStreamResult))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetUserImage(Guid id)
        {
            try
            {
                RServiceResult<RImage> img = await _appUserService.GetUserImage(id);

                if (!string.IsNullOrEmpty(img.ExceptionString))
                {
                    return BadRequest(img.ExceptionString);

                }
                if (img.Result == null)
                    return NotFound();

                Response.GetTypedHeaders().LastModified = img.Result.LastModified;

                var requestHeaders = Request.GetTypedHeaders();
                if (requestHeaders.IfModifiedSince.HasValue &&
                    requestHeaders.IfModifiedSince.Value >= img.Result.LastModified)
                {
                    return StatusCode(StatusCodes.Status304NotModified);
                }

                RServiceResult<string> imgPath = _imageFileService.GetImagePath(img.Result);
                if (!string.IsNullOrEmpty(imgPath.ExceptionString))
                    return BadRequest(imgPath.ExceptionString);


                return new FileStreamResult(new FileStream(imgPath.Result, FileMode.Open, FileAccess.Read), img.Result.ContentType);


            }
            catch (Exception exp)
            {
                return BadRequest(exp.ToString());
            }
        }

        /// <summary>
        /// returns user roles (if user does not have user:view permission trying to view other users' information fails with a forbidden error)
        /// </summary>
        /// <param name="id">user id</param>
        /// <returns>user roles</returns>
        [HttpGet("{id}/roles")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> GetUserRoles(Guid id)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

            if (loggedOnUserId != id)
            {
                RServiceResult<bool> canViewAllUsersInformation =
                    await _userPermissionChecker.Check
                    (
                        loggedOnUserId,
                        new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value),
                        SecurableItem.UserEntityShortName,
                        SecurableItem.ViewAllOperationShortName
                        );
                if (!string.IsNullOrEmpty(canViewAllUsersInformation.ExceptionString))
                    return BadRequest(canViewAllUsersInformation.ExceptionString);

                if (!canViewAllUsersInformation.Result)
                    return Forbid();
            }


            RServiceResult<IList<string>> roles = await _appUserService.GetUserRoles(id);
            if (!string.IsNullOrEmpty(roles.ExceptionString))
                return BadRequest(roles.ExceptionString);

            return Ok(roles.Result.ToArray());
        }

        /// <summary>
        /// remove user from role
        /// </summary>
        /// <param name="id">user id</param>
        /// <param name="role"></param>
        /// <returns>true if succeeds</returns>
        [HttpDelete("{id}/roles/{role}")]
        [Authorize(Policy = SecurableItem.UserEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> RemoveFromRole(Guid id, string role)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            if (loggedOnUserId == id)
            {
                return BadRequest("You cannot modify your own roles.");
            }
            RServiceResult<bool> isAdmin = await _appUserService.IsAdmin(loggedOnUserId);
            if (!string.IsNullOrEmpty(isAdmin.ExceptionString))
                return BadRequest(isAdmin.ExceptionString);
            if (!isAdmin.Result)
            {
                RServiceResult<bool> isDeletingUserAdmin = await _appUserService.IsAdmin(id);
                if (!string.IsNullOrEmpty(isDeletingUserAdmin.ExceptionString))
                    return BadRequest(isDeletingUserAdmin.ExceptionString);
                if (isDeletingUserAdmin.Result)
                    return Forbid();//You can not delete admin users roles.
            }



            RServiceResult<bool> res = await _appUserService.RemoveFromRole(id, role);
            if (!res.Result)
            {
                return BadRequest(res.ExceptionString);
            }

            return Ok(true);
        }
        /// <summary>
        /// add user to role
        /// </summary>
        /// <param name="id">user id</param>
        /// <param name="role"></param>
        /// <returns>true if succeeds</returns>
        [HttpPost("{id}/roles/{role}")]
        [Authorize(Policy = SecurableItem.UserEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> AddToRole(Guid id, string role)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            if (loggedOnUserId == id)
            {
                return BadRequest("You cannot modify your own roles.");
            }
            RServiceResult<bool> isAdmin = await _appUserService.IsAdmin(loggedOnUserId);
            if (!string.IsNullOrEmpty(isAdmin.ExceptionString))
                return BadRequest(isAdmin.ExceptionString);
            if (!isAdmin.Result)
            {
                RServiceResult<bool> isDeletingUserAdmin = await _appUserService.IsAdmin(id);
                if (!string.IsNullOrEmpty(isDeletingUserAdmin.ExceptionString))
                    return BadRequest(isDeletingUserAdmin.ExceptionString);
                if (isDeletingUserAdmin.Result)
                    return Forbid();//You can not delete admin users roles.
            }



            RServiceResult<bool> res = await _appUserService.AddToRole(id, role);
            if (!res.Result)
            {
                return BadRequest(res.ExceptionString);
            }

            return Ok(true);
        }

        /// <summary>
        /// get a captcha image for signup or forgot password
        /// </summary>        
        /// <returns>captchaimageid - display it using api/rimages/captchaimageid.jpg</returns>
        [HttpGet]
        [AllowAnonymous]
        [Route("captchaimage")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(Guid))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]

        public async Task<IActionResult> GenerateCaptchImage()
        {

            RServiceResult<RImage> img = await _captchaService.Generate();

            if (!string.IsNullOrEmpty(img.ExceptionString))
            {
                return BadRequest(img.ExceptionString);
            }

            return Ok(img.Result.Id);
        }


        /// <summary>
        /// signup
        /// </summary>
        /// <param name="signUpViewModel">signUpViewModel</param>
        /// <returns>next step: "verify" or "finalize"</returns>
        [HttpPost]
        [AllowAnonymous]
        [Route("signup")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> SignUp([FromBody] UnverifiedSignUpViewModel signUpViewModel)
        {
            if (SiteInReadOnlyMode)
                return BadRequest("سایت به دلایل فنی مثل انتقال سرور موقتاً در حالت فقط خواندنی قرار دارد. لطفاً ساعاتی دیگر مجدداً برای ثبت نام تلاش کنید.");

            if (!SignupEnabled)
                return BadRequest("ثبت نام غیرفعال است.");

            RServiceResult<bool> captchaRes = await _captchaService.Evaluate(signUpViewModel.CaptchaImageId, signUpViewModel.CaptchaValue);
            if (!string.IsNullOrEmpty(captchaRes.ExceptionString))
                return BadRequest(captchaRes.ExceptionString);

            if (!captchaRes.Result)
                return BadRequest("مقدار تصویر امنیتی درست وارد نشده است.");

            var bannedEmail = await _appUserService.GetBannedEmailInformationAsync(signUpViewModel.Email);
            if (bannedEmail.Result != null)
                return BadRequest("ایمیل شما در لیست سیاه قرار دارد و نمی‌توانید با آن مجدداً ثبت نام کنید.");

            string clientIPAddress = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
            RServiceResult<RVerifyQueueItem> res = await _appUserService.SignUp(signUpViewModel.Email, clientIPAddress, signUpViewModel.ClientAppName, signUpViewModel.Language);
            if (res.Result == null)
            {
                return BadRequest(res.ExceptionString);
            }

            try
            {
                await _emailSender.SendEmailAsync
                    (
                    signUpViewModel.Email,
                    _appUserService.GetEmailSubject(RVerifyQueueType.SignUp, res.Result.Secret),
                    _appUserService.GetEmailHtmlContent(RVerifyQueueType.SignUp, res.Result.Secret, signUpViewModel.CallbackUrl)
                    );
                return Ok("verify");
            }
            catch (Exception exp)
            {
                return BadRequest("Error sending email: " + exp.ToString());
            }
        }

        /// <summary>
        /// verify signup / forgot password / self delete user
        /// </summary>
        /// <param name="type"></param>
        /// <param name="secret"></param>
        /// <returns>associated secret email</returns> 
        [HttpGet]
        [AllowAnonymous]
        [Route("verify")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> VerifySignUp(RVerifyQueueType type, string secret)
        {

            RServiceResult<string> res = await _appUserService.RetrieveEmailFromQueueSecret(type, secret);

            if (!string.IsNullOrEmpty(res.ExceptionString))
            {
                return BadRequest(res.ExceptionString);
            }
            if (string.IsNullOrWhiteSpace(res.Result))
            {
                return NotFound("رمز وارد شده تطابق ندارد");
            }
            return Ok(res.Result);
        }

        /// <summary>
        /// finalize signup process
        /// </summary>
        /// <param name="newUserInfo"></param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        [Route("finalizesignup")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> FinalizeSignUp([AuditIgnore][FromBody] VerifiedSignUpViewModel newUserInfo)
        {
            RServiceResult<bool> result = await _appUserService.FinalizeSignUp(newUserInfo.Email, newUserInfo.Secret, newUserInfo.Password, newUserInfo.FirstName, newUserInfo.SureName);
            if (!result.Result)
                return BadRequest(result.ExceptionString);
            return Ok(true);
        }

        /// <summary>
        /// start forgot password process by email
        /// </summary>
        /// <param name="fpwdViewModel">signUpViewModel</param>
        /// <returns>result</returns>
        [HttpPost]
        [AllowAnonymous]
        [Route("forgotpassword")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> ForgotPassword(
            [FromBody]
            UnverifiedSignUpViewModel fpwdViewModel
            )
        {

            RServiceResult<bool> captchaRes = await _captchaService.Evaluate(fpwdViewModel.CaptchaImageId, fpwdViewModel.CaptchaValue);
            if (!string.IsNullOrEmpty(captchaRes.ExceptionString))
                return BadRequest(captchaRes.ExceptionString);

            if (!captchaRes.Result)
                return BadRequest("مقدار تصویر امنیتی درست وارد نشده است.");

            string clientIPAddress = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
            RServiceResult<RVerifyQueueItem> res = await _appUserService.ForgotPassword(fpwdViewModel.Email, clientIPAddress, fpwdViewModel.ClientAppName, fpwdViewModel.Language);
            if (res.Result == null)
            {
                return BadRequest(res.ExceptionString);
            }

            try
            {
                await _emailSender.SendEmailAsync
                    (
                    fpwdViewModel.Email,
                    _appUserService.GetEmailSubject(RVerifyQueueType.ForgotPassword, res.Result.Secret),
                    _appUserService.GetEmailHtmlContent(RVerifyQueueType.ForgotPassword, res.Result.Secret, fpwdViewModel.CallbackUrl)
                    );
            }
            catch (Exception exp)
            {
                return BadRequest("Error sending email: " + exp.ToString());
            }

            return Ok(true);
        }

        /// <summary>
        /// read only mode
        /// </summary>
        public bool SiteInReadOnlyMode
        {
            get
            {
                return bool.Parse(Configuration["ReadOnlyMode"]);
            }
        }


        /// <summary>
        /// Is Sign-up enabled?
        /// </summary>
        /// <returns></returns>
        public bool SignupEnabled
        {
            get
            {
                return bool.Parse(Configuration.GetSection("SignUp")["Enabled"]);
            }
        }

        /// <summary>
        /// reset password
        /// </summary>
        /// <param name="pwd"></param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        [Route("resetpassword")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> ResetPassword([AuditIgnore][FromBody] ResetPasswordViewModel pwd)
        {
            string clientIPAddress = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
            RServiceResult<bool> result = await _appUserService.ResetPassword(pwd.Email, pwd.Secret, pwd.Password, clientIPAddress);
            if (!result.Result)
                return BadRequest(result.ExceptionString);
            return Ok(true);
        }

        /// <summary>
        /// log user bad behaviuor
        /// </summary>
        /// <param name="userCause"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Policy = SecurableItem.UserEntityShortName + ":" + SecurableItem.Administer)]
        [Route("behaviour/log")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RUserBehaviourLog))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> LogUserBehaviourAsync([FromBody] UserCauseViewModel userCause)
        {
            RServiceResult<RUserBehaviourLog> result = await _appUserService.LogUserBehaviourAsync(userCause.UserId, userCause.Cause);
            if (!string.IsNullOrEmpty(result.ExceptionString))
                return BadRequest(result.ExceptionString);
            return Ok(result.Result);
        }

        /// <summary>
        /// kick out a user
        /// </summary>
        /// <param name="userCause"></param>
        /// <returns></returns>

        [HttpPost]
        [Authorize(Policy = SecurableItem.UserEntityShortName + ":" + SecurableItem.Administer)]
        [Route("kickout")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RUserBehaviourLog))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> KickOutUserAsync([FromBody] UserCauseViewModel userCause)
        {
            RServiceResult<PublicRAppUser> userInfo = await _appUserService.GetUserInformation(userCause.UserId);
            if (userInfo.Result == null)
            {
                if (string.IsNullOrEmpty(userInfo.ExceptionString))
                    return NotFound();
                return BadRequest(userInfo.ExceptionString);
            }

            await _appUserService.BanUserFromSigningUpAgainAsync(userCause.UserId, userCause.Cause);
            await _appUserService.DeleteUser(userCause.UserId);

            try
            {
                await _emailSender.SendEmailAsync
                    (
                    userInfo.Result.Email,
                    _appUserService.GetEmailSubject(RVerifyQueueType.KickOutUser, ""),
                    _appUserService.GetEmailHtmlContent(RVerifyQueueType.KickOutUser, userCause.Cause, "")
                    );
                return Ok();
            }
            catch (Exception exp)
            {
                return BadRequest("Error sending email: " + exp.ToString());
            }
        }

        /// <summary>
        /// IAppUserService instance
        /// </summary>
        protected IAppUserService _appUserService;

        /// <summary>
        /// for client IP resolution
        /// </summary>
        protected IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// IUserPermissionChecker instance
        /// </summary>
        protected IUserPermissionChecker _userPermissionChecker;

        /// <summary>
        /// IEmailSender instance
        /// </summary>
        protected IEmailSender _emailSender;

        /// <summary>
        /// Image File Service
        /// </summary>
        protected readonly IImageFileService _imageFileService;

        /// <summary>
        /// Captcha service
        /// </summary>
        protected readonly ICaptchaService _captchaService;

        /// <summary>
        /// Configuration
        /// </summary>
        protected IConfiguration Configuration { get; }



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
        public AppUserControllerBase(IConfiguration configuration, IAppUserService appUserService, IHttpContextAccessor httpContextAccessor, IUserPermissionChecker userPermissionChecker, IEmailSender emailSender, IImageFileService imageFileService, ICaptchaService captchaService)
        {
            Configuration = configuration;
            _appUserService = appUserService;
            _userPermissionChecker = userPermissionChecker;
            _httpContextAccessor = httpContextAccessor;
            _emailSender = emailSender;
            _imageFileService = imageFileService;
            _captchaService = captchaService;
        }
    }
}
