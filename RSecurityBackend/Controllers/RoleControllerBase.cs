using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using RSecurityBackend.Models.Auth.Db;
using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Models.Generic;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using RSecurityBackend.Services;

namespace RSecurityBackend.Controllers
{
    /// <summary>
    /// User roles
    /// </summary>
    [Produces("application/json")]
    [Route("api/roles")]
    public abstract class RoleControllerBase : Controller
    {
        /// <summary>
        /// All Roles Information
        /// </summary>
        /// <returns>All Roles Information</returns>
        [HttpGet]
        [Authorize(Policy = SecurableItem.RoleEntityShortName + ":" + SecurableItem.ViewOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<RAppRole>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> Get()
        {

            RServiceResult<RAppRole[]> rolesInfo = await _roleService.GetAllRoles();
            if (rolesInfo.Result == null)
            {
                return BadRequest(rolesInfo.ExceptionString);
            }
            return Ok(rolesInfo.Result);
        }

        /// <summary>
        /// returns role information
        /// </summary>
        /// <param name="roleName">role name</param>
        /// <returns>role information</returns>
        [HttpGet("{roleName}")]
        [Authorize(Policy = SecurableItem.RoleEntityShortName + ":" + SecurableItem.ViewOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RAppRole))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Get(string roleName)
        {
            RServiceResult<RAppRole> roleInfo = await _roleService.GetRoleInformation(roleName);
            if (roleInfo.Result == null)
            {
                if (string.IsNullOrEmpty(roleInfo.ExceptionString))
                    return NotFound();
                return BadRequest(roleInfo.ExceptionString);
            }
            return Ok(roleInfo.Result);
        }

        /// <summary>
        /// add a new role
        /// </summary>
        /// <param name="newGroupInfo"></param>
        /// <returns>id if required could be retrieved from return value</returns>
        [HttpPost]
        [Authorize(Policy = SecurableItem.RoleEntityShortName + ":" + SecurableItem.AddOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RAppRole))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> Post([FromBody]RAppRole newGroupInfo)
        {            
            RServiceResult<RAppRole> result = await _roleService.AddRole(newGroupInfo);
            if (result.Result == null)
                return BadRequest(result.ExceptionString);
            return Ok(result.Result);
        }

        /// <summary>
        /// update existing role
        /// </summary>
        /// <param name="roleName">role name</param>
        /// <param name="existingGroupInfo">existingGroupInfo.id could be passed empty and it is ignored completely</param>
        /// <returns>true if succeeds</returns>
        [HttpPut("{roleName}")]
        [Authorize(Policy = SecurableItem.RoleEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> Put(string roleName, [FromBody]RAppRole existingGroupInfo)
        {          

            RServiceResult<bool> res = await _roleService.ModifyRole(roleName, existingGroupInfo);
            if (!res.Result)
                return BadRequest(res.ExceptionString);

            return Ok(true);

        }

        /// <summary>
        /// delete role
        /// </summary>
        /// <param name="roleName">role name</param>
        /// <returns>true if succeeds</returns>
        [HttpDelete("{roleName}")]
        [Authorize(Policy = SecurableItem.RoleEntityShortName + ":" + SecurableItem.DeleteOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> Delete(string roleName)
        {          

            RServiceResult<bool> res = await _roleService.DeleteRole(roleName);
            if (!res.Result)
            {
                return BadRequest(res.ExceptionString);
            }

            return Ok(true);
        }

        /// <summary>
        /// lists role permissions
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize(Policy = SecurableItem.RoleEntityShortName + ":" + SecurableItem.ViewOperationShortName)]
        [Route("permissions/{roleName}")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<SecurableItem[]>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetRoleSecurableItemsStatus(string roleName)
        {
            RServiceResult<SecurableItem[]> res = await _roleService.GetRoleSecurableItemsStatus(roleName);

            if (res.Result == null)
            {
                return BadRequest(res.ExceptionString);
            }
            return Ok(res.Result);
        }


        /// <summary>
        /// Saves role permissions
        /// </summary>
        /// <param name="roleName">role name</param>
        /// <param name="securableItems"></param>
        /// <returns></returns>
        [HttpPut]
        [Authorize(Policy = SecurableItem.RoleEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [Route("permissions/{roleName}")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<SecurableItem[]>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> SetRoleSecurableItemsStatus(string roleName, [FromBody]SecurableItem[] securableItems)
        {
            RServiceResult<bool> res = await _roleService.SetRoleSecurableItemsStatus(roleName, securableItems);

            if (!res.Result)
            {
                return BadRequest(res.ExceptionString);
            }
            return Ok(res.Result);
        }

        /// <summary>
        /// Get All SecurableItems
        /// </summary>
        /// <returns>All All SecurableItems</returns>
        [HttpGet]
        [Route("securableitems")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<SecurableItem>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public IActionResult GetSecurableItems()
        {
            return Ok(_roleService.GetSecurableItems());
        }


        /// <summary>
        /// IUserRoleService instance
        /// </summary>
        protected IUserRoleService _roleService;


        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="roleService"></param>
        public RoleControllerBase(IUserRoleService roleService)
        {
            _roleService = roleService;          
        }
    }
}
