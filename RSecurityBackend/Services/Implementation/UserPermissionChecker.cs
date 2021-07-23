using RSecurityBackend.Models.Auth.Db;
using RSecurityBackend.Models.Auth.ViewModels;
using RSecurityBackend.Models.Generic;
using System;
using System.Threading.Tasks;

namespace RSecurityBackend.Services.Implementation
{
    /// <summary>
    /// Permission checker service implementation
    /// </summary>
    public class UserPermissionChecker : IUserPermissionChecker
    {
        /// <summary>
        /// check to see if user has permission to do operation
        /// </summary>
        /// <param name="userId">userId</param>
        /// <param name="sessionId"></param>
        /// <param name="securableItemShortName">form</param>
        /// <param name="operationShortName">operation</param>
        /// <returns>true if has permission</returns>
        public virtual async Task<RServiceResult<bool>> Check(Guid userId, Guid sessionId, string securableItemShortName, string operationShortName)
        {
            RServiceResult<PublicRAppUser> userInfo = await _appUserService.GetUserInformation(userId);
            if (userInfo.Result == null)
            {
                return new RServiceResult<bool>(false);
            }
            if (userInfo.Result.Status != RAppUserStatus.Active)
            {
                return new RServiceResult<bool>(false);
            }
            RServiceResult<bool> sessionCheckResult = await _appUserService.SessionExists(userId, sessionId);
            if (!string.IsNullOrEmpty(sessionCheckResult.ExceptionString))
            {
                return new RServiceResult<bool>(false, sessionCheckResult.ExceptionString);
            }

            if (!sessionCheckResult.Result)
            {
                return new RServiceResult<bool>(false);
            }

            RServiceResult<bool> isAdminResult = await _appUserService.IsAdmin(userId);
            if (!string.IsNullOrEmpty(isAdminResult.ExceptionString))
            {
                return new RServiceResult<bool>(false, isAdminResult.ExceptionString);
            }

            if (isAdminResult.Result)
            {
                return new RServiceResult<bool>(true);
            }
            RServiceResult<bool> hasPermission =
                await _appUserService.HasPermission(userId, securableItemShortName, operationShortName);

            if (hasPermission.Result)
            {
                return new RServiceResult<bool>(true);
            }
            return new RServiceResult<bool>(false);
        }

        /// <summary>
        /// IAppUserService instance
        /// </summary>
        protected IAppUserService _appUserService;




        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="appUserService"></param>
        public UserPermissionChecker(IAppUserService appUserService)
        {
            _appUserService = appUserService;
        }
    }
}
