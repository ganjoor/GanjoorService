using Microsoft.AspNetCore.Authorization;

namespace RSecurityBackend.Authorization
{
    /// <summary>
    /// UserGroupPermissionRequirement
    /// </summary>
    public class UserGroupPermissionRequirement : IAuthorizationRequirement
    {
        /// <summary>
        ///
        /// </summary>
        /// <see cref="RSecurityBackend.Models.Auth.Memory.SecurableItem.ShortName"/>
        /// <example>
        /// job
        /// </example>
        public string SecurableItemShortName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <see cref="RSecurityBackend.Models.Auth.Memory.SecurableItemOperation.ShortName"/>
        /// <example>
        /// view
        /// </example>
        public string OperationShortName { get; set; }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="securableItemShortName"></param>
        /// <param name="operationShortName"></param>
        public UserGroupPermissionRequirement(string securableItemShortName, string operationShortName)
        {
            SecurableItemShortName = securableItemShortName;
            OperationShortName = operationShortName;
        }
    }
}
