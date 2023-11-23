using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RMuseum.Models.Auth.Memory;
using RMuseum.Models.GanjoorAudio;
using RMuseum.Models.GanjoorAudio.ViewModels;
using RMuseum.Models.UploadSession;
using RMuseum.Models.UploadSession.ViewModels;
using RMuseum.Services;
using RMuseum.Services.Implementation;
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
        /// returns paginated published recitations (if poetId or catId is non-zero its ordered by poemId ascending if not it is ordered by publish date descending)
        /// </summary>
        /// <param name="paging">if PageSize is -1 or is more than 1000 it resets to 1000</param>
        /// <param name="searchTerm">empty: no search term, non-empty: searches within AudioArtist, AudioTitle, poem.FullTitle and poem.PlainText simultaneously</param>
        /// <param name="poetId"></param>
        /// <param name="catId"></param>
        /// <returns></returns>
        [HttpGet("published")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<PublicRecitationViewModel>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetPublished([FromQuery] PagingParameterModel paging, string searchTerm = "", int poetId = 0, int catId = 0)
        {
            if (paging.PageSize == -1 || paging.PageSize > 1000)
                paging.PageSize = 1000;
            var res = await _audioService.GetPublishedRecitations(paging, searchTerm, poetId, catId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);

            // Paging Header
            HttpContext.Response.Headers.Append("paging-headers", JsonConvert.SerializeObject(res.Result.PagingMeta));

            return Ok(res.Result.Items);
        }


        /// <summary>
        /// get published recitation by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("published/{id}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(PublicRecitationViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound, Type = typeof(string))]
        public async Task<IActionResult> GetPublishedRecitationById(int id)
        {
            var res = await _audioService.GetPublishedRecitationById(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);

            if (res.Result == null)
                return NotFound();
          
            return Ok(res.Result);
        }

        /// <summary>
        /// creates an RSS file from recent published recitations
        /// </summary>
        /// <returns></returns>
        [HttpGet("published/rss")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(FileStreamResult))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetRssFeed()
        {
            int count = 200;
            var res = await _audioService.GetPublishedRecitations(new PagingParameterModel() { PageNumber = 1, PageSize = count});
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);

            string rss = RecitationsRssBuilder.Build(res.Result.Items);
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(rss);
            writer.Flush();
            stream.Position = 0;

            return new FileStreamResult(stream, "application/rss+xml");
        }

        /// <summary>
        /// Gets audio narrations, user must have recitation::moderate permission to be able to see all users narrations
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="allUsers">default: false, user must have recitation::moderate permission to be able to see all users narrations</param>
        /// <param name="status">default: -1, unfiltered</param>
        /// <param name="searchTerm"></param>
        /// <param name="mistakes"></param>
        /// <remarks>additional headers: paging-headers, audio-upload-enabled</remarks>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<RecitationViewModel>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden, Type = typeof(string))]
        public async Task<IActionResult> Get([FromQuery] PagingParameterModel paging, bool allUsers = false, AudioReviewStatus status = AudioReviewStatus.All, string searchTerm = "", bool mistakes = false)
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
                         User.Claims.Any(c => c.Type == "Language") ? User.Claims.First(c => c.Type == "Language").Value : "fa-IR",
                         RMuseumSecurableItem.AudioRecitationEntityShortName,
                         RMuseumSecurableItem.ModerateOperationShortName
                         );
                if (!string.IsNullOrEmpty(canView.ExceptionString))
                    return BadRequest(canView.ExceptionString);

                if (!canView.Result)
                    return StatusCode((int)HttpStatusCode.Forbidden);
            }

            var res = await _audioService.SecureGetAll(paging, allUsers ? Guid.Empty : loggedOnUserId, status, searchTerm, mistakes);
            if(!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);

            // Paging Header
            HttpContext.Response.Headers.Append("paging-headers", JsonConvert.SerializeObject(res.Result.PagingMeta));

            //adding audio upload enabled header to reduce need for a separate query
            HttpContext.Response.Headers.Append("audio-upload-enabled", JsonConvert.SerializeObject(_audioService.UploadEnabled));

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
        [ProducesResponseType((int)HttpStatusCode.NotFound, Type = typeof(string))]
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

            HttpContext.Response.Headers.Append("Accept-Ranges", "bytes");

            return new FileStreamResult(new FileStream(narration.Result.LocalMp3FilePath, FileMode.Open, FileAccess.Read), "audio/mpeg");
        }

        

        /// <summary>
        /// get the corresponding xml file contents (xml) for the narration
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
        [ProducesResponseType((int)HttpStatusCode.NotFound, Type = typeof(string))]
        public async Task<IActionResult> GetXMLContent(int id)
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
        /// get the corresponding xml file for the recitation
        /// </summary>
        /// <remarks>
        /// it could be protected (Authorized), but I guess I would have problems with available client components support,
        /// so I preferred it to be anonymous, as it does not harm anybody I guess 
        /// </remarks>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("file/{id}.xml")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(FileStreamResult))]
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

            return new FileStreamResult(new FileStream(narration.Result.LocalXmlFilePath, FileMode.Open, FileAccess.Read), "text/xml");
        }

        /// <summary>
        /// Gets Verse Sync Information
        /// </summary>
        /// <param name="id">narration id</param>
        /// <returns></returns>
        [HttpGet("verses/{id}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RecitationVerseSync[]))]
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
        [ProducesResponseType((int)HttpStatusCode.NotFound, Type = typeof(string))]
        public async Task<IActionResult> UpdatePoemNarration(int id, [FromBody] RecitationViewModel metadata)
        {
            if (!_audioService.UploadEnabled)
                return BadRequest("این قابلیت به دلیل تغییرات فنی سایت موقتاً غیرفعال است.");

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
                         User.Claims.Any(c => c.Type == "Language") ? User.Claims.First(c => c.Type == "Language").Value : "fa-IR",
                         RMuseumSecurableItem.AudioRecitationEntityShortName,
                         RMuseumSecurableItem.ModerateOperationShortName
                         );
                if (!string.IsNullOrEmpty(serviceResult.ExceptionString))
                    return BadRequest(serviceResult.ExceptionString);

                if (!serviceResult.Result)
                    return StatusCode((int)HttpStatusCode.Forbidden);

            }

            if(narration.Result.ReviewStatus != metadata.ReviewStatus)
            {
                if (metadata.ReviewStatus == AudioReviewStatus.Approved || metadata.ReviewStatus == AudioReviewStatus.Rejected || metadata.ReviewStatus == AudioReviewStatus.RejectedDueToReportedErrors)
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
        /// Moderate pending narration (for moderating other users' recitations you also need recitation:moderate permission)
        /// </summary>
        /// <param name="id"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("moderate/{id}")]
        [Authorize(Policy = RMuseumSecurableItem.AudioRecitationEntityShortName + ":" + RMuseumSecurableItem.PublishOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RecitationViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound, Type = typeof(string))]
        public async Task<IActionResult> ModeratePoemNarration(int id, [FromBody] RecitationModerateViewModel model)
        {
            if (!_audioService.UploadEnabled)
                return BadRequest("این قابلیت به دلیل تغییرات فنی سایت موقتاً غیرفعال است.");

            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

            var narration = await _audioService.Get(id);
            if (!string.IsNullOrEmpty(narration.ExceptionString))
            {
                return BadRequest(narration.ExceptionString);
            }

            if (narration.Result == null)
                return NotFound();

            if(narration.Result.ReviewStatus == AudioReviewStatus.RejectedDueToReportedErrors)
                return StatusCode((int)HttpStatusCode.Forbidden);

            if (narration.Result.Owner.Id != loggedOnUserId)
            {
                Guid sessionId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value);
                RServiceResult<bool>
                 serviceResult =
                 await _userPermissionChecker.Check
                     (
                         loggedOnUserId,
                         sessionId,
                         User.Claims.Any(c => c.Type == "Language") ? User.Claims.First(c => c.Type == "Language").Value : "fa-IR",
                         RMuseumSecurableItem.AudioRecitationEntityShortName,
                         RMuseumSecurableItem.ModerateOperationShortName
                         );
                if (!string.IsNullOrEmpty(serviceResult.ExceptionString))
                    return BadRequest(serviceResult.ExceptionString);

                if (!serviceResult.Result)
                    return StatusCode((int)HttpStatusCode.Forbidden);

            }

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
            if (!_audioService.UploadEnabled)
                return BadRequest("این قابلیت به دلیل تغییرات فنی سایت موقتاً غیرفعال است.");

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
        /// <param name="allUsers">default: false, user must have recitation::moderate permission to be able to see all users uploads</param>
        /// <returns></returns>
        /// <remarks>additional headers: paging-headers, audio-upload-enabled</remarks>
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
                         User.Claims.Any(c => c.Type == "Language") ? User.Claims.First(c => c.Type == "Language").Value : "fa-IR",
                         RMuseumSecurableItem.AudioRecitationEntityShortName,
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
            HttpContext.Response.Headers.Append("paging-headers", JsonConvert.SerializeObject(res.Result.PagingMeta));

            //adding audio upload enabled header to reduce need for a separate query
            HttpContext.Response.Headers.Append("audio-upload-enabled", JsonConvert.SerializeObject(_audioService.UploadEnabled));

            return Ok(res.Result.Items);
        }

        /// <summary>
        /// upload, update, moderate and delete operations on recitations might temporarily become disabled,
        /// this method gets the current status
        /// remarks: the value of this flag is provided as a custom header called audio-upload-enabled in some common GET methods
        /// in order to reduce the need for a separate query
        /// </summary>
        /// <returns></returns>

        [HttpGet("uploadenabled")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        public IActionResult IsUploadEnabled()
        {
            return Ok(_audioService.UploadEnabled);
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
                if (!_audioService.UploadEnabled)
                    return BadRequest("ارسال خوانش جدید به دلیل تغییرات فنی سایت موقتاً غیرفعال است.");
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
        /// Get User Profiles
        /// </summary>
        /// <param name="artistName"></param>
        /// <returns></returns>
        [HttpGet("profile")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<UserRecitationProfileViewModel>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden, Type = typeof(string))]

        public async Task<IActionResult> GetUserNarrationProfiles(string artistName)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            var res = await _audioService.GetUserNarrationProfiles(loggedOnUserId, artistName);
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
        /// <param name="paging"></param>
        /// <param name="unfinished"></param>
        /// <returns></returns>

        [HttpGet("publishqueue")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<RecitationPublishingTracker>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden, Type = typeof(string))]
        public async Task<IActionResult> GetPublishingQueueStatus([FromQuery] PagingParameterModel paging, bool unfinished)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            Guid sessionId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value);
            RServiceResult<bool>
                 canViewAll =
                 await _userPermissionChecker.Check
                     (
                         loggedOnUserId,
                         sessionId,
                         User.Claims.Any(c => c.Type == "Language") ? User.Claims.First(c => c.Type == "Language").Value : "fa-IR",
                         RMuseumSecurableItem.AudioRecitationEntityShortName,
                         RMuseumSecurableItem.ModerateOperationShortName
                         );
            if (!string.IsNullOrEmpty(canViewAll.ExceptionString))
                return BadRequest(canViewAll.ExceptionString);

            var res = await _audioService.GetPublishingQueueStatus(paging, unfinished, canViewAll.Result ? Guid.Empty : loggedOnUserId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            // Paging Header
            HttpContext.Response.Headers.Append("paging-headers", JsonConvert.SerializeObject(res.Result.PagingMeta));

            return Ok(res.Result.Items);
        }

        /// <summary>
        /// Transfer Recitations Ownership (for recitations owned by current user)
        /// </summary>
        /// <param name="targetEmailAddress"></param>
        /// <param name="artistName"></param>
        /// <returns>number of transfered items</returns>

        [HttpPut("chown")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(int))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound, Type = typeof(string))]
        public async Task<IActionResult> TransferRecitationsOwnership(string targetEmailAddress, string artistName)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            var userSearchResult = await _appUserService.FindUserByEmail(targetEmailAddress);
            if (!string.IsNullOrEmpty(userSearchResult.ExceptionString))
                return BadRequest(userSearchResult);
            if (userSearchResult.Result == null)
                return NotFound();

            var resExec = await _audioService.TransferRecitationsOwnership(loggedOnUserId, (Guid)userSearchResult.Result.Id, artistName);
            if (!string.IsNullOrEmpty(resExec.ExceptionString))
                return BadRequest(resExec.ExceptionString);

            return Ok(resExec.Result);

        }

        
        /// <summary>
        /// Synchronization Queue
        /// </summary>
        /// <returns></returns>
        [HttpGet("syncqueue")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<RecitationViewModel>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden, Type = typeof(string))]
        public async Task<IActionResult> GetSynchronizationQueue()
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            Guid sessionId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value);
            RServiceResult<bool>
                 canViewAll =
                 await _userPermissionChecker.Check
                     (
                         loggedOnUserId,
                         sessionId,
                         User.Claims.Any(c => c.Type == "Language") ? User.Claims.First(c => c.Type == "Language").Value : "fa-IR",
                         RMuseumSecurableItem.AudioRecitationEntityShortName,
                         RMuseumSecurableItem.ModerateOperationShortName
                         );
            if (!string.IsNullOrEmpty(canViewAll.ExceptionString))
                return BadRequest(canViewAll.ExceptionString);
            var res = await _audioService.GetSynchronizationQueue(canViewAll.Result ? Guid.Empty : loggedOnUserId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
           
            return Ok(res.Result);
        }

        /// <summary>
        /// report an error in a recitation
        /// </summary>
        /// <param name="report"></param>
        /// <returns></returns>
        [HttpPost("errors/report")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RecitationErrorReportViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden, Type = typeof(string))]
        public async Task<IActionResult> ReportErrorAsync([FromBody] RecitationErrorReportViewModel report)
        {
            if (ReadOnlyMode)
                return BadRequest("سایت به دلایل فنی مثل انتقال سرور موقتاً در حالت فقط خواندنی قرار دارد. لطفاً ساعاتی دیگر مجدداً تلاش کنید.");

            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            var res = await _audioService.ReportErrorAsync(loggedOnUserId, report);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get errors reported for recitations
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>
        [HttpGet("errors/report")]
        [Authorize(Policy = RMuseumSecurableItem.AudioRecitationEntityShortName + ":" + RMuseumSecurableItem.ModerateOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<RecitationErrorReportViewModel>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetReportedErrorsAsync([FromQuery] PagingParameterModel paging)
        {
            var reports = await _audioService.GetReportedErrorsAsync(paging);
            if (!string.IsNullOrEmpty(reports.ExceptionString))
            {
                return BadRequest(reports.ExceptionString);
            }

            // Paging Header
            HttpContext.Response.Headers.Append("paging-headers", JsonConvert.SerializeObject(reports.Result.PagingMeta));

            return Ok(reports.Result.Items);
        }

        /// <summary>
        /// reject a reported error for recitations and notify the reporter (and deletes the report)
        /// </summary>
        /// <param name="id"></param>
        /// <param name="rejectionNote"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("errors/report/{id}")]
        [Authorize(Policy = RMuseumSecurableItem.AudioRecitationEntityShortName + ":" + RMuseumSecurableItem.ModerateOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(int))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> RejectReportedErrorAsync(int id, string rejectionNote = "عدم تطابق با معیارهای حذف خوانش")
        {
            if (ReadOnlyMode)
                return BadRequest("سایت به دلایل فنی مثل انتقال سرور موقتاً در حالت فقط خواندنی قرار دارد. لطفاً ساعاتی دیگر مجدداً تلاش کنید.");
            RServiceResult<bool> res = await _audioService.RejectReportedErrorAsync(id, rejectionNote);
            if (!string.IsNullOrEmpty(res.ExceptionString))
            {
                return BadRequest(res.ExceptionString);
            }
            if (!res.Result)
            {
                return NotFound();
            }
            return Ok();
        }

        /// <summary>
        /// accepts a reported error for recitations, change status of the recitation to rejected and notify the reporter and recitation owner (and deletes the report)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>

        [HttpDelete]
        [Route("errors/report/accept/{id}")]
        [Authorize(Policy = RMuseumSecurableItem.AudioRecitationEntityShortName + ":" + RMuseumSecurableItem.ModerateOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(int))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> AcceptReportedErrorAsync(int id)
        {
            if (ReadOnlyMode)
                return BadRequest("سایت به دلایل فنی مثل انتقال سرور موقتاً در حالت فقط خواندنی قرار دارد. لطفاً ساعاتی دیگر مجدداً تلاش کنید.");
            RServiceResult<bool> res = await _audioService.AcceptReportedErrorAsync(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
            {
                return BadRequest(res.ExceptionString);
            }
            if (!res.Result)
            {
                return NotFound();
            }
            return Ok();
        }

        /// <summary>
        /// accepts a reported error for recitations, add mistake to approve the mistake and notify the reporter and recitation owner (and deletes the report)
        /// </summary>
        /// <param name="report"></param>
        /// <returns></returns>

        [HttpPut]
        [Route("errors/report/save")]
        [Authorize(Policy = RMuseumSecurableItem.AudioRecitationEntityShortName + ":" + RMuseumSecurableItem.ModerateOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(int))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> AddReportToTheApprovedMistakesAsync([FromBody] RecitationErrorReportViewModel report)
        {
            if (ReadOnlyMode)
                return BadRequest("سایت به دلایل فنی مثل انتقال سرور موقتاً در حالت فقط خواندنی قرار دارد. لطفاً ساعاتی دیگر مجدداً تلاش کنید.");
            RServiceResult<bool> res = await _audioService.AddReportToTheApprovedMistakesAsync(report);
            if (!string.IsNullOrEmpty(res.ExceptionString))
            {
                return BadRequest(res.ExceptionString);
            }
            if (!res.Result)
            {
                return NotFound();
            }
            return Ok();
        }

        /// <summary>
        /// up vote a recitation
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost("vote/{id}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> UpVoteRecitationAsync(int id)
        {
            if (ReadOnlyMode)
                return BadRequest("سایت به دلایل فنی مثل انتقال سرور موقتاً در حالت فقط خواندنی قرار دارد. لطفاً ساعاتی دیگر مجدداً تلاش کنید.");

            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            var res = await _audioService.UpVoteRecitationAsync(id, loggedOnUserId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok();
        }

        /// <summary>
        /// revoke recitaion up vote
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("vote/{id}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> RevokeUpVoteFromRecitationAsync(int id)
        {
            if (ReadOnlyMode)
                return BadRequest("سایت به دلایل فنی مثل انتقال سرور موقتاً در حالت فقط خواندنی قرار دارد. لطفاً ساعاتی دیگر مجدداً تلاش کنید.");

            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            var res = await _audioService.RevokeUpVoteFromRecitationAsync(id, loggedOnUserId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok();
        }

        /// <summary>
        /// revoke recitaion up vote
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPut("vote/switch/{id}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> SwitchRecitationUpVoteAsync(int id)
        {
            if (ReadOnlyMode)
                return BadRequest("سایت به دلایل فنی مثل انتقال سرور موقتاً در حالت فقط خواندنی قرار دارد. لطفاً ساعاتی دیگر مجدداً تلاش کنید.");

            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            var res = await _audioService.SwitchRecitationUpVoteAsync(id, loggedOnUserId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get user upvoted recitations
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>
        [HttpGet("votes")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<PublicRecitationViewModel>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetUserUpvotedRecitationsAsync([FromQuery] PagingParameterModel paging)
        {
            if (paging.PageSize == -1 || paging.PageSize > 1000)
                paging.PageSize = 1000;
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            var res = await _audioService.GetUserUpvotedRecitationsAsync(paging, loggedOnUserId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);

            // Paging Header
            HttpContext.Response.Headers.Append("paging-headers", JsonConvert.SerializeObject(res.Result.PagingMeta));

            return Ok(res.Result.Items);
        }

        /// <summary>
        /// compute poem recitations order (no update)
        /// </summary>
        /// <param name="poemId"></param>
        /// <returns></returns>
        [HttpGet("votes/{poemId}/scores")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<RecitationOrderingViewModel>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> ComputePoemRecitationsOrdersAsync(int poemId)
        {
            var res = await _audioService.ComputePoemRecitationsOrdersAsync(poemId, false);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);

            return Ok(res.Result);
        }

        /// <summary>
        /// starts checking recitaions with missing files and add them to reported errors list job
        /// </summary>
        /// <returns></returns>
        [HttpPost("healthcheck")]
        [Authorize(Policy = RMuseumSecurableItem.AudioRecitationEntityShortName + ":" + RMuseumSecurableItem.ModerateOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public IActionResult StartCheckingRecitationsHealthCheck()
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            var res = _audioService.StartCheckingRecitationsHealthCheck(loggedOnUserId);

            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);

            return Ok();
        }

        /// <summary>
        /// retry publish unpublished narrations
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("retrypublish")]
        [Authorize(Policy = RMuseumSecurableItem.AudioRecitationEntityShortName + ":" + RMuseumSecurableItem.PublishOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> RetryPublish()
        {
            try
            {
                if (!_audioService.UploadEnabled)
                    return BadRequest("این قابلیت به دلیل تغییرات فنی سایت موقتاً غیرفعال است.");

                await _audioService.RetryPublish();
                return Ok();
            }
            catch (Exception exp)
            {
                return BadRequest(exp.ToString());
            }

        }

        /// <summary>
        /// Makes recitations of فریدون فرح‌اندوز first recitations
        /// </summary>
        /// <returns></returns>
        [HttpPut("ff")]
        [Authorize(Policy = RMuseumSecurableItem.AudioRecitationEntityShortName + ":" + RMuseumSecurableItem.ImportOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(int))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound, Type = typeof(string))]
        public async Task<IActionResult> MakeFFRecitationsFirst()
        {
            var resExec = await _audioService.MakeArtistRecitationsFirst("فریدون فرح‌اندوز");
            if (!string.IsNullOrEmpty(resExec.ExceptionString))
                return BadRequest(resExec.ExceptionString);
            return Ok(resExec.Result);
        }

        /// <summary>
        /// get category top one recitations
        /// </summary>
        /// <param name="catId"></param>
        /// <param name="includePoemText"></param>
        /// <returns></returns>
        [HttpGet("cattop1/{catId}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<PublicRecitationViewModel>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetPoemCategoryTopRecitations(int catId, bool includePoemText)
        {
            var res = await _audioService.GetPoemCategoryTopRecitations(catId, includePoemText);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        ///  check if a category has any recitations
        /// </summary>
        /// <param name="catId"></param>
        /// <returns></returns>
        [HttpGet("catany/{catId}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetPoemCategoryHasAnyRecitations(int catId)
        {
            var res = await _audioService.GetPoemCategoryHasAnyRecitations(catId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// rss for category top one recitations
        /// </summary>
        /// <param name="catId"></param>
        /// <param name="includePoemText"></param>
        /// <returns></returns>

        [HttpGet("cattop1/{catId}/rss")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(FileStreamResult))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetPoemCategoryTopRecitationsRSS(int catId, bool includePoemText)
        {
            var res = await _audioService.GetPoemCategoryTopRecitations(catId, includePoemText);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);

            string rss = RecitationsRssBuilder.Build(res.Result);
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(rss);
            writer.Flush();
            stream.Position = 0;

            return new FileStreamResult(stream, "application/rss+xml");
        }

        /// <summary>
        /// readonly mode
        /// </summary>
        public bool ReadOnlyMode
        {
            get
            {
                try
                {
                    return bool.Parse(Configuration["ReadOnlyMode"]);
                }
                catch
                {
                    return false;
                }
            }
        }


        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="audioService"></param>
        /// <param name="userPermissionChecker"></param>
        /// <param name="appUserService"></param>
        /// <param name="configuration"></param>
        public RecitationController(IRecitationService audioService, IUserPermissionChecker userPermissionChecker, IAppUserService appUserService, IConfiguration configuration)
        {
            _audioService = audioService;
            _userPermissionChecker = userPermissionChecker;
            _appUserService = appUserService;
        }

        /// <summary>
        /// Artifact Service
        /// </summary>
        protected readonly IRecitationService _audioService;

        /// <summary>
        /// IUserPermissionChecker instance
        /// </summary>
        protected IUserPermissionChecker _userPermissionChecker;

        /// <summary>
        /// IAppUserService instance
        /// </summary>
        protected IAppUserService _appUserService;

        /// <summary>
        /// Configuration
        /// </summary>
        protected IConfiguration Configuration { get; }

    }
}
