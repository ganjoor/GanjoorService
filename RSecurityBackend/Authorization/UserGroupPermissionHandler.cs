using Microsoft.AspNetCore.Authorization;
using RSecurityBackend.Models.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using RSecurityBackend.Services;

namespace RSecurityBackend.Authorization
{
    /// <summary>
    /// UserGroupPermissionHandler
    /// </summary>
    public class UserGroupPermissionHandler : AuthorizationHandler<UserGroupPermissionRequirement>
    {
        /// <summary>
        /// HandleRequirementAsync
        /// </summary>
        /// <param name="context"></param>
        /// <param name="requirement"></param>
        /// <returns></returns>
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, UserGroupPermissionRequirement requirement)
        {
          
            if (!context.User.HasClaim(c => c.Type == "UserId") || !context.User.HasClaim(c => c.Type == "SessionId"))
            {
                context.Fail();
                return;
            }

            RServiceResult<bool> result = await _userPermissionChecker.Check
                (
                new Guid(context.User.Claims.FirstOrDefault(c => c.Type == "UserId").Value),
                new Guid(context.User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value),
                requirement.SecurableItemShortName,
                requirement.OperationShortName                
                );

            if(result.Result)
            {
                context.Succeed(requirement);
                return;
            }

            context.Fail();
           
        }


        /// <summary>
        /// IUserPermissionChecker instance
        /// </summary>
        private IUserPermissionChecker _userPermissionChecker;

              
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="userPermissionChecker"></param>
        public UserGroupPermissionHandler(IUserPermissionChecker userPermissionChecker) : base()
        {
            _userPermissionChecker = userPermissionChecker;         
        }
    }
}
