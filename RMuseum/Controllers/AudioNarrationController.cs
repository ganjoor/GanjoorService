using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RMuseum.Models.Auth.Memory;
using RMuseum.Models.GanjoorAudio;
using RMuseum.Models.GanjoorAudio.ViewModels;
using RMuseum.Models.UploadSession;
using RMuseum.Models.UploadSession.ViewModels;
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
        /// Get User Uploads
        /// </summary>
        /// <param name="paging">default: false, user must have narration::moderate permission to be able to see all users uploads</param>
        /// <param name="allUsers"></param>
        /// <returns></returns>
        [HttpGet("uploads")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<UploadSessionViewModel>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden, Type = typeof(string))]

        public async Task<IActionResult> GetUploads([FromQuery] PagingParameterModel paging, bool allUsers = false)
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

            var res = await _audioService.GetUploads(paging, allUsers ? Guid.Empty : loggedOnUserId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
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
        /// Get User Profiles
        /// </summary>
        /// <returns></returns>
        [HttpGet("profile")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<UserNarrationProfileViewModel>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden, Type = typeof(string))]

        public async Task<IActionResult> GetUserNarrationProfiles()
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);


            var res = await _audioService.GetUserNarrationProfiles(loggedOnUserId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);


            return Ok(res.Result);
        }

        /// <summary>
        /// Add a narration profile
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        [HttpPost("profile")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(UserNarrationProfileViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden, Type = typeof(string))]

        public async Task<IActionResult> AddUserNarrationProfiles([FromBody]UserNarrationProfileViewModel profile)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

            profile.UserId = loggedOnUserId;


            var res = await _audioService.AddUserNarrationProfiles(profile);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);


            return Ok(res.Result);
        }

        /// <summary>
        /// Update a narration profile
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        [HttpPut("profile")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(UserNarrationProfileViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden, Type = typeof(string))]

        public async Task<IActionResult> UpdateUserNarrationProfiles([FromBody] UserNarrationProfileViewModel profile)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

            profile.UserId = loggedOnUserId;

            var res = await _audioService.UpdateUserNarrationProfiles(profile);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);


            return Ok(res.Result);
        }

        /// <summary>
        /// Delete a narration profile 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>

        [HttpDelete("profile/{id}")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> DeleteUserNarrationProfiles(Guid id)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<bool> res = await _audioService.DeleteUserNarrationProfiles(id, loggedOnUserId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
            {
                return BadRequest(res.ExceptionString);
            }
            return Ok(res.Result);
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
