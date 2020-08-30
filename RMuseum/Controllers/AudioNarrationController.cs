using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RMuseum.Models.Auth.Memory;
using RMuseum.Services;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace RMuseum.Controllers
{
    [Produces("application/json")]
    [Route("api/audio")]
    public class AudioNarrationController : Controller
    {

        /// <summary>
        /// imports data from ganjoor MySql database
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("mysqlonetimeimport")]
        [Authorize(Policy = RMuseumSecurableItem.AudioNarrationEntityShortName + ":" + RMuseumSecurableItem.ImportOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> MysqlonetimeImport()
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<bool> res =
                await _audioService.OneTimeImport(loggedOnUserId);
            if (res.Result)
                return Ok();
            return BadRequest(res.ExceptionString);
        }


        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="audioService">
        /// </param>
        /// <param name="userPermissionChecker"></param>
        public AudioNarrationController(IAudioNarrationService audioService, IUserPermissionChecker userPermissionChecker)
        {
            _audioService = audioService;
            _userPermissionChecker = userPermissionChecker;
        }

        /// <summary>
        /// Artifact Service
        /// </summary>
        protected readonly IAudioNarrationService _audioService;

        /// <summary>
        /// IUserPermissionChecker instance
        /// </summary>
        protected IUserPermissionChecker _userPermissionChecker;
    }
}
