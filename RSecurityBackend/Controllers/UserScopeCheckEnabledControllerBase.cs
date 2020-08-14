using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services;


namespace RSecurityBackend.Controllers
{
    /// <summary>
    /// base class for controllers needing UserScopeCheck
    /// </summary>
    public abstract class UserScopeCheckEnabledControllerBase : Controller
    {
        /// <summary>
        /// UserScopeCheck method EntityShortName;
        /// </summary>
        /// <example>
        /// RBillingSecurableItem.TenantEntityShortName
        /// </example>
        protected abstract string UserScopeCheckEntityShortName { get; }

        /// <summary>
        /// UserScopeCheck method OperationShortName
        /// </summary>
        /// <example>
        /// SecurableItem.ViewOperationShortName
        /// </example>
        protected abstract string UserScopeCheckOperationShortName { get; }


        /// <summary>
        /// returns user id if user is a guest, and empty if he/she has access to view all users information
        /// </summary>
        /// <returns></returns>
        protected async Task<RServiceResult<Guid>> GetUserIdUnlessUserHavePermissionToReadAllUsersDataWhichReturnEmptyUserId()
        {
            Guid userId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

            RServiceResult<bool> canReadAllUsersData =
                await _userPermissionChecker.Check
                (
                    userId,
                    new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value),
                    UserScopeCheckEntityShortName,
                    UserScopeCheckOperationShortName
                    
                    );

            if (!string.IsNullOrEmpty(canReadAllUsersData.ExceptionString))
                return new RServiceResult<Guid>(userId, canReadAllUsersData.ExceptionString);
            if (canReadAllUsersData.Result)
            {
                userId = Guid.Empty;
            }
            return new RServiceResult<Guid>(userId);
        }

        /// <summary>
        /// can current user read this userId daya
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        protected async Task<RServiceResult<bool>> CanReadUserData(Guid userId)
        {
            RServiceResult<Guid> scopeForUserId = await GetUserIdUnlessUserHavePermissionToReadAllUsersDataWhichReturnEmptyUserId();
            if(!string.IsNullOrEmpty(scopeForUserId.ExceptionString))
            {
                return new RServiceResult<bool>(false, scopeForUserId.ExceptionString);
            }
            return new RServiceResult<bool>(scopeForUserId.Result == Guid.Empty || scopeForUserId.Result == userId);
        }

        /// <summary>
        /// Permission Checker Service
        /// </summary>
        protected readonly IUserPermissionChecker _userPermissionChecker;


        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="userPermissionChecker"></param>
        public UserScopeCheckEnabledControllerBase(IUserPermissionChecker userPermissionChecker)
        {
            _userPermissionChecker = userPermissionChecker;
        }


    }
}
