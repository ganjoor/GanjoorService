using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
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

            var cachKey = _GetUserLevelCachKey(loggedOnUserId, name);
            if (!_memoryCache.TryGetValue(cachKey, out string val))
            {
                var res = await _optionsService.GetValueAsync(name, loggedOnUserId);
                if (!string.IsNullOrEmpty(res.ExceptionString))
                    return BadRequest(res.ExceptionString);
                val = res.Result;
                _memoryCache.Set(cachKey, val);
            }
           
            return Ok(val);
        }

        private string _GetUserLevelCachKey(Guid loggedOnUserId, string name)
        {
            return $"RGenericOptionsControllerBase::GetValue::{loggedOnUserId}::{name}";
        }

        /// <summary>
        /// get user level option, Security Warning: every authenticated user could see value of global options, so do not store sensitive data into them
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
            _memoryCache.Set(_GetUserLevelCachKey(loggedOnUserId, name), value);
            return Ok(res.Result);
        }

        /// <summary>
        /// get global option value, Security Warning: every authenticated user could see value of global options, so do not store sensitive data into them
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>

        [HttpGet("global/{name}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetGlobalOptionValue(string name)
        {
            var cachKey = _GetGlobalCachKey(name);
            if (!_memoryCache.TryGetValue(cachKey, out string val))
            {
                var res = await _optionsService.GetValueAsync(name, null);
                if (!string.IsNullOrEmpty(res.ExceptionString))
                    return BadRequest(res.ExceptionString);
                val = res.Result;
                _memoryCache.Set(cachKey, val);
            }
            return Ok(val);
        }

        private string _GetGlobalCachKey(string name)
        {
            return $"RGenericOptionsControllerBase::GetGlobalOptionValue::{name}";
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
            _memoryCache.Set(_GetGlobalCachKey(name), value);
            return Ok(res.Result);
        }

       /// <summary>
       /// constructor
       /// </summary>
       /// <param name="optionsService"></param>
       /// <param name="memoryCache"></param>
        public RGenericOptionsControllerBase(IRGenericOptionsService optionsService, IMemoryCache memoryCache)
        {
            _optionsService = optionsService;
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// Options Service
        /// </summary>
        protected readonly IRGenericOptionsService _optionsService;

        /// <summary>
        /// IMemoryCache
        /// </summary>
        protected readonly IMemoryCache _memoryCache;
    }
}
