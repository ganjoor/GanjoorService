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
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace RMuseum.Controllers
{
    [Produces("application/json")]
    [Route("api/audio")]
    public class RecitationController : Controller
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
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<RecitationViewModel>))]
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
        /// get the corresponding mp3 file for the narration
        /// </summary>
        /// <remarks>
        /// it could be protected (Authorized), but I guess I would have problems with available client components support,
        /// so I preferred it to be anonymous, as it does not harm anybody I guess 
        /// </remarks>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("file/{id}.mp3")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(FileStreamResult))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetMp3File(int id)
        {
            RServiceResult<RecitationViewModel> narration =
                await _audioService.Get(id);

            if (!string.IsNullOrEmpty(narration.ExceptionString))
                return BadRequest(narration.ExceptionString);

            if (narration.Result == null)
                return NotFound();

            Response.GetTypedHeaders().LastModified = narration.Result.UploadDate;//TODO: Add a FileLastUpdated field to narrations to indicate the last time the mp3/xml files have been updated

            var requestHeaders = Request.GetTypedHeaders();
            if (requestHeaders.IfModifiedSince.HasValue &&
                requestHeaders.IfModifiedSince.Value >= narration.Result.UploadDate)
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            return new FileStreamResult(new FileStream(narration.Result.LocalMp3FilePath, FileMode.Open, FileAccess.Read), "audio/mpeg");
        }

        /// <summary>
        /// get the corresponding xml file contemnts (xml) for the narration
        /// </summary>
        /// <remarks>
        /// it could be protected (Authorized), but I guess I would have problems with available client components support,
        /// so I preferred it to be anonymous, as it does not harm anybody I guess 
        /// </remarks>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("xml/{id}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetXMLFile(int id)
        {
            RServiceResult<RecitationViewModel> narration =
                await _audioService.Get(id);

            if (!string.IsNullOrEmpty(narration.ExceptionString))
                return BadRequest(narration.ExceptionString);

            if (narration.Result == null)
                return NotFound();

            Response.GetTypedHeaders().LastModified = narration.Result.UploadDate;//TODO: Add a FileLastUpdated field to narrations to indicate the last time the mp3/xml files have been updated

            var requestHeaders = Request.GetTypedHeaders();
            if (requestHeaders.IfModifiedSince.HasValue &&
                requestHeaders.IfModifiedSince.Value >= narration.Result.UploadDate)
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            return Ok(System.IO.File.ReadAllText(narration.Result.LocalXmlFilePath));
        }

        /// <summary>
        /// Gets Verse Sync Information
        /// </summary>
        /// <param name="id">narration id</param>
        /// <returns></returns>
        [HttpGet("verses/{id}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetPoemNarrationVerseSyncArray(int id)
        {
            RServiceResult<RecitationVerseSync[]> verses =
                await _audioService.GetPoemNarrationVerseSyncArray(id);
            if (!string.IsNullOrEmpty(verses.ExceptionString))
                return BadRequest(verses.ExceptionString);
            return Ok(verses.Result);
        }

        /// <summary>
        /// updates narration metadata
        /// </summary>
        /// <param name="id"></param>
        /// <param name="metadata"></param>
        /// <remarks>
        /// reviewstatus cannot be set to Approved or Rejected using this method, use moderate method instead
        /// only these set of fields are updatable: AudioTitle, AudioArtist, AudioArtistUrl, AudioSrc, AudioSrcUrl, ReviewStatus (Draft to Pending and vice versa and Approved/Rejected to Pending)
        /// only narrator or a moderator can update the narration 
        /// </remarks>
        /// <returns></returns>

        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RecitationViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden, Type = typeof(string))]
        public async Task<IActionResult> UpdatePoemNarration(int id, [FromBody] RecitationViewModel metadata)
        {
           

            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

            var narration = await _audioService.Get(id);
            if (!string.IsNullOrEmpty(narration.ExceptionString))
            {
               return BadRequest(narration.ExceptionString);
            }
            
            if (narration.Result == null)
                return NotFound();

            if(narration.Result.Owner.Id != loggedOnUserId)
            {
                Guid sessionId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value);
                RServiceResult<bool>
                 serviceResult =
                 await _userPermissionChecker.Check
                     (
                         loggedOnUserId,
                         sessionId,
                         RMuseumSecurableItem.AudioNarrationEntityShortName,
                         RMuseumSecurableItem.ModerateOperationShortName
                         );
                if (!string.IsNullOrEmpty(serviceResult.ExceptionString))
                    return BadRequest(serviceResult.ExceptionString);

                if (!serviceResult.Result)
                    return StatusCode((int)HttpStatusCode.Forbidden);

            }

            if(narration.Result.ReviewStatus != metadata.ReviewStatus)
            {
                if (metadata.ReviewStatus == AudioReviewStatus.Approved || metadata.ReviewStatus == AudioReviewStatus.Rejected)
                {
                    return StatusCode((int)HttpStatusCode.Forbidden);
                }
            }

            var res = await _audioService.UpdatePoemNarration(id, metadata);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);

            return Ok(res.Result);
        }

        /// <summary>
        /// Moderate pending narration
        /// </summary>
        /// <param name="id"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("moderate/{id}")]
        [Authorize(Policy = RMuseumSecurableItem.AudioNarrationEntityShortName + ":" + RMuseumSecurableItem.ModerateOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RecitationViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden, Type = typeof(string))]
        public async Task<IActionResult> ModeratePoemNarration(int id, [FromBody] RecitationModerateViewModel model)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);


            var res = await _audioService.ModeratePoemNarration(id, loggedOnUserId, model);
            if (!string.IsNullOrEmpty(res.ExceptionString))
            {
                if(res.ExceptionString == "404")
                {
                    return NotFound();
                }
                return BadRequest(res.ExceptionString);
            }

            return Ok(res.Result);
        }

        /// <summary>
        /// Delete a recitation
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> DeleteRecitation(int id)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<bool> res = await _audioService.Delete(id, loggedOnUserId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
            {
                return BadRequest(res.ExceptionString);
            }
            return Ok(res.Result);
        }



        /// <summary>
        /// Get User Uploads
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="allUsers">default: false, user must have narration::moderate permission to be able to see all users uploads</param>
        /// <returns></returns>
        [HttpGet("uploads")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<UploadedItemViewModel>))]
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
        /// Reciation Upload
        /// </summary>
        /// <param name="replace">
        /// if you send true to replace parameter, if there is an existing recitation for the poem from the user with the same Audio Artist name
        /// corresponding mp3+xml files are replaced an no other changes is applied (no new post, preserving recitation position)
        /// </param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(UploadSession))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden, Type = typeof(string))]
        public async Task<IActionResult> UploadReciation(bool replace = false)
        {
            try
            {
                Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
                RServiceResult<UploadSession> resSession = await _audioService.InitiateNewUploadSession(loggedOnUserId, replace);
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
        /// retry publish unpublished narrations
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("retrypublish")]
        [Authorize(Policy = RMuseumSecurableItem.AudioNarrationEntityShortName + ":" + RMuseumSecurableItem.ModerateOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public IActionResult RetryPublish()
        {
            try
            {
                _audioService.RetryPublish();
                return Ok();
            }
            catch(Exception exp)
            {
                return BadRequest(exp.ToString());
            }     
            
        }

        /// <summary>
        /// Get User Profiles
        /// </summary>
        /// <returns></returns>
        [HttpGet("profile")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<UserRecitationProfileViewModel>))]
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
        /// Get User Default Profile
        /// </summary>
        /// <returns></returns>
        [HttpGet("profile/def")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(UserRecitationProfileViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden, Type = typeof(string))]

        public async Task<IActionResult> GetUserDefProfile()
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            var res = await _audioService.GetUserDefProfile(loggedOnUserId);
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
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(UserRecitationProfileViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden, Type = typeof(string))]

        public async Task<IActionResult> AddUserNarrationProfiles([FromBody]UserRecitationProfileViewModel profile)
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
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(UserRecitationProfileViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden, Type = typeof(string))]

        public async Task<IActionResult> UpdateUserNarrationProfiles([FromBody] UserRecitationProfileViewModel profile)
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
        [Authorize]
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
        /// publishing tracker data
        /// </summary>
        /// <remarks>TODO: Poem Narration Data is not purified yet (includes sensitive information), so it needs a very special permission</remarks>
        /// <param name="paging"></param>
        /// <param name="inProgress"></param>
        /// <param name="finished"></param>
        /// <returns></returns>

        [HttpGet("publishqueue")]
        [Authorize(Policy = RMuseumSecurableItem.AudioNarrationEntityShortName + ":" + RMuseumSecurableItem.ImportOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<RecitationPublishingTracker>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden, Type = typeof(string))]
        public async Task<IActionResult> GetPublishingQueueStatus([FromQuery] PagingParameterModel paging, bool inProgress = true, bool finished = true)
        {
            var res = await _audioService.GetPublishingQueueStatus(paging, inProgress, finished);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            // Paging Header
            HttpContext.Response.Headers.Add("paging-headers", JsonConvert.SerializeObject(res.Result.PagingMeta));

            return Ok(res.Result.Items);
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="audioService"></param>
        /// <param name="userPermissionChecker"></param>
        public RecitationController(IRecitationService audioService, IUserPermissionChecker userPermissionChecker)
        {
            _audioService = audioService;
            _userPermissionChecker = userPermissionChecker;
        }

        /// <summary>
        /// Artifact Service
        /// </summary>
        protected readonly IRecitationService _audioService;

        /// <summary>
        /// IUserPermissionChecker instance
        /// </summary>
        protected IUserPermissionChecker _userPermissionChecker;

    }
}
