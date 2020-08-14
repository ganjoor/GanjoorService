using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using RSecurityBackend.Services;
using RSecurityBackend.Controllers;
using Audit.WebApi;

namespace RMuseum.Controllers
{
    /// <summary>
    ///roles
    /// </summary>
    [Produces("application/json")]
    [Route("api/roles")]
    public class RoleController : RoleControllerBase
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="roleService"></param>
        public RoleController(IUserRoleService roleService)
            : base(roleService)
        {
        }
    }
}
