using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RMuseum.Models.Auth.Memory;
using RMuseum.Models.GanjoorAudio;
using RMuseum.Models.GanjoorAudio.ViewModels;
using RMuseum.Models.UploadSession;
using RMuseum.Services;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services;
using System;
using System.Collections.Generic;
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
        /// Gets audio narrations, user must have narration::moderate permission to be able to see all users narrations
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="allUsers">default: false, user must have narration::moderate permission to be able to see all users narrations</param>
        /// <param name="status">default: -1, unfiltered</param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<PoemNarrationViewModel>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden, Type = typeof(string))]

        public async Task<IActionResult> Get([FromQuery] PagingParameterModel paging, bool allUsers = false, AudioReviewStatus status = AudioReviewStatus.All )
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            Guid sessionId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value);

            if (allUsers)
            {
                RServiceResult<bool>
                 canView =
                 await _userPermissionChecker.Check
                     (
                         loggedOnUserId,
                         sessionId,
                         RMuseumSecurableItem.AudioNarrationEntityShortName,
                         RMuseumSecurableItem.ModerateOperationShortName
                         );
                if (!string.IsNullOrEmpty(canView.ExceptionString))
                    return BadRequest(canView.ExceptionString);

                if (!canView.Result)
                    return StatusCode((int)HttpStatusCode.Forbidden);
            }

            var res = await _audioService.GetAll(paging, allUsers ? Guid.Empty : loggedOnUserId, status);
            if(!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);

            // Paging Header
            HttpContext.Response.Headers.Add("paging-headers", JsonConvert.SerializeObject(res.Result.PagingMeta));

            return Ok(res.Result.Items);
        }

        /// <summary>
        /// Narration Upload
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(UploadSession))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden, Type = typeof(string))]
        public async Task<IActionResult> UploadNarrations()
        {
            try
            {
                Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
                RServiceResult<UploadSession> resSession = await _audioService.InitiateNewUploadSession(loggedOnUserId);
                if (!string.IsNullOrEmpty(resSession.ExceptionString))
                    return BadRequest(resSession.ExceptionString);
                List<UploadSessionFile> files = new List<UploadSessionFile>();
                foreach(IFormFile file in Request.Form.Files)
                {
                    RServiceResult<UploadSessionFile> rsFileResult = await _audioService.SaveUploadedFile(file);
                    if(!string.IsNullOrEmpty(rsFileResult.ExceptionString))
                    {
                        return BadRequest(rsFileResult.ExceptionString);
                    }
                    files.Add(rsFileResult.Result);
                }
                resSession = await _audioService.FinalizeNewUploadSession(resSession.Result, files.ToArray());

                return Ok(resSession.Result);
            }
            catch (Exception exp)
            {
                return BadRequest(exp.ToString());
            }
        }


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
