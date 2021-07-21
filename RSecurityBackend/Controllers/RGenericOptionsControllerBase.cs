using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Services;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;


namespace RSecurityBackend.Controllers
{
    /// <summary>
    /// options controller
    /// </summary>
    [Produces("application/json")]
    [Route("api/options")]
    public abstract class RGenericOptionsControllerBase : Controller
    {
        /// <summary>
        /// get user level option
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpGet("{name}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetValue(string name)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            var res = await _optionsService.GetValueAsync(name, loggedOnUserId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get user level option
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [HttpPut("{name}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> SetValue(string name, [FromBody] string value)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            var res = await _optionsService.SetAsync(name, value, loggedOnUserId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get global option value
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>

        [HttpGet("global/{name}")]
        [Authorize(Policy = SecurableItem.GlobalOptionsEntityShortName + ":" + SecurableItem.ViewOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetGlobalOptionValue(string name)
        {
            var res = await _optionsService.GetValueAsync(name, null);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// set global option value
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [HttpPut("global/{name}")]
        [Authorize(Policy = SecurableItem.GlobalOptionsEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> SetGlobalOptionValue(string name, [FromBody] string value)
        {
            var res = await _optionsService.SetAsync(name, value, null);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="optionsService">
        /// </param>
        public RGenericOptionsControllerBase(IRGenericOptionsService optionsService)
        {
            _optionsService = optionsService;
        }

        /// <summary>
        /// Options Service
        /// </summary>
        protected readonly IRGenericOptionsService _optionsService;
    }
}
