using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RMuseum.Models.Auth.Memory;
using RMuseum.Models.Auth.ViewModel;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Models.GanjoorAudio.ViewModels;
using RMuseum.Models.GanjoorIntegration.ViewModels;
using RMuseum.Services;
using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Models.Auth.ViewModels;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Models.Image;
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
    [Route("api/ganjoor")]
    public class GanjoorController : Controller
    {
        /// <summary>
        /// get list of poets
        /// </summary>
        /// <param name="websitePoets"></param>
        /// <param name="includeBio"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("poets")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPoetViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetPoets(bool websitePoets = true, bool includeBio = true)
        {
            RServiceResult<GanjoorPoetViewModel[]> res =
                await _ganjoorService.GetPoets(websitePoets, includeBio);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// poet by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("poet/{id}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPoetCompleteViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetPoetById(int id)
        {
            RServiceResult<GanjoorPoetCompleteViewModel> res =
                await _ganjoorService.GetPoetById(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            if (res.Result == null)
                return NotFound();
            return Ok(res.Result);
        }

        /// <summary>
        /// poet by url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("poet")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPoetCompleteViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetPoetByUrl(string url)
        {
            RServiceResult<GanjoorPoetCompleteViewModel> res =
                await _ganjoorService.GetPoetByUrl(url);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            if (res.Result == null)
                return NotFound();
            return Ok(res.Result);
        }

        /// <summary>
        /// get poet image
        /// </summary>
        /// <param name="url">sample: hafez</param>
        /// <returns></returns>
        [HttpGet("poet/image/{url}.png")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(FileStreamResult))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> Get(string url)
        {

            RServiceResult<Guid> poet = await _ganjoorService.GetPoetImageIdByUrl($"/{url}");
            if (!string.IsNullOrEmpty(poet.ExceptionString))
                return BadRequest(poet.ExceptionString);

            if (poet.Result == Guid.Empty)
                return NotFound();


            RServiceResult<RImage> img =
                await _imageFileService.GetImage(poet.Result);

            if (!string.IsNullOrEmpty(img.ExceptionString))
                return BadRequest(img.ExceptionString);

            if (img.Result == null)
                return NotFound();

            Response.GetTypedHeaders().LastModified = img.Result.LastModified;

            var requestHeaders = Request.GetTypedHeaders();
            if (requestHeaders.IfModifiedSince.HasValue &&
                requestHeaders.IfModifiedSince.Value >= img.Result.LastModified)
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            RServiceResult<string> imgPath = _imageFileService.GetImagePath(img.Result);
            if (!string.IsNullOrEmpty(imgPath.ExceptionString))
                return BadRequest(imgPath.ExceptionString);


            return new FileStreamResult(new FileStream(imgPath.Result, FileMode.Open, FileAccess.Read), "image/png");

        }

        /// <summary>
        /// cat by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="poems"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("cat/{id}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPoetCompleteViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetCatById(int id, bool poems = true)
        {
            RServiceResult<GanjoorPoetCompleteViewModel> res =
                await _ganjoorService.GetCatById(id, poems);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            if (res.Result == null)
                return NotFound();
            return Ok(res.Result);
        }

        /// <summary>
        /// cat by url
        /// </summary>
        /// <param name="url"></param>
        /// <param name="poems"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("cat")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPoetCompleteViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetCatByUrl(string url, bool poems = true)
        {
            RServiceResult<GanjoorPoetCompleteViewModel> res =
                await _ganjoorService.GetCatByUrl(url, poems);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            if (res.Result == null)
                return NotFound();
            return Ok(res.Result);
        }

        /// <summary>
        /// page by url
        /// </summary>
        /// <param name="url"></param>
        /// <param name="catPoems"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("page")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPageCompleteViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetPageByUrl(string url, bool catPoems = false)
        {
            RServiceResult<GanjoorPageCompleteViewModel> res =
                await _ganjoorService.GetPageByUrl(url, catPoems);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            if (res.Result == null)
                return NotFound();
            return Ok(res.Result);
        }


        /// <summary>
        /// modify page
        /// </summary>
        /// <param name="id"></param>
        /// <param name="page"></param>
        /// <returns></returns>

        [HttpPut]
        [Route("page/{id}")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPageCompleteViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> ModifyPage(int id, [FromBody]GanjoorModifyPageViewModel page)
        {
            Guid userId =
               new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

            RServiceResult<GanjoorPageCompleteViewModel> res =
                await _ganjoorService.ModifyPage(id, userId, page);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            if (res.Result == null)
                return NotFound();
            return Ok(res.Result);
        }

        /// <summary>
        /// page url by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("pageurl")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetPageUrlById(int id)
        {
            RServiceResult<string> res =
                await _ganjoorService.GetPageUrlById(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            if (string.IsNullOrEmpty(res.Result))
                return NotFound();
            return Ok(res.Result);
        }

        /// <summary>
        /// get poem by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="catInfo"></param>
        /// <param name="catPoems"></param>
        /// <param name="rhymes"></param>
        /// <param name="recitations"></param>
        /// <param name="images"></param>
        /// <param name="songs"></param>
        /// <param name="comments">not implemented yet</param>
        /// <param name="verseDetails"></param>
        /// <param name="navigation">next/previous</param>
        /// <returns></returns>
        [HttpGet]
        [Route("poem/{id}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPoemCompleteViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetPoemById(int id, bool catInfo = true, bool catPoems = false, bool rhymes = true, bool recitations = true, bool images = true, bool songs = true, bool comments = true, bool verseDetails = true, bool navigation = true)
        {
            RServiceResult<GanjoorPoemCompleteViewModel> res =
                await _ganjoorService.GetPoemById(id, catInfo, catPoems, rhymes, recitations, images, songs, comments, verseDetails, navigation);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            if (res.Result == null)
                return NotFound();
            return Ok(res.Result);
        }

        /// <summary>
        /// get poem by url
        /// </summary>
        /// <param name="url"></param>
        /// <param name="catInfo"></param>
        /// <param name="catPoems"></param>
        /// <param name="rhymes">not implemented yet</param>
        /// <param name="recitations"></param>
        /// <param name="images"></param>
        /// <param name="songs">not implemented yet</param>
        /// <param name="comments">not implemented yet</param>
        /// <param name="verseDetails"></param>
        /// <param name="navigation">next/previous</param>
        /// <returns></returns>
        [HttpGet]
        [Route("poem")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPoemCompleteViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetPoemByUrl(string url, bool catInfo = true, bool catPoems = false, bool rhymes = true, bool recitations = true, bool images = true, bool songs = true, bool comments = true, bool verseDetails = true, bool navigation = true)
        {
            RServiceResult<GanjoorPoemCompleteViewModel> res =
                await _ganjoorService.GetPoemByUrl(url, catInfo, catPoems, rhymes, recitations, images, songs, comments, verseDetails, navigation);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            if (res.Result == null)
                return NotFound();
            return Ok(res.Result);
        }

        /// <summary>
        ///  get poem recitations  (PlainText/HtmlText are intentionally empty)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("poem/{id}/recitations")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(PublicRecitationViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetPoemRecitations(int id)
        {
            RServiceResult<PublicRecitationViewModel[]> res =
                await _ganjoorService.GetPoemRecitations(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        ///  get poem images  (PlainText/HtmlText are intentionally empty)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("poem/{id}/images")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(PoemRelatedImage[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetPoemImages(int id)
        {
            RServiceResult<PoemRelatedImage[]> res =
                await _ganjoorService.GetPoemImages(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get poem songs
        /// </summary>
        /// <param name="id"></param>
        /// <param name="approved"></param>
        /// <param name="trackType"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("poem/{id}/songs")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(PoemMusicTrackViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetPoemSongs(int id, bool approved, PoemMusicTrackType trackType = PoemMusicTrackType.All)
        {
            RServiceResult<PoemMusicTrackViewModel[]> res =
                await _ganjoorService.GetPoemSongs(id, approved, trackType);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// suggest song for poem
        /// </summary>
        /// <param name="song"></param>
        /// <returns></returns>

        [HttpPost]
        [Route("song")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(PoemMusicTrackViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> SuggestSong([FromBody] PoemMusicTrackViewModel song)
        {
            Guid userId =
                 new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            Guid sessionId =
                new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value);
            RServiceResult<bool> sessionCheckResult = await _appUserService.SessionExists(userId, sessionId);
            if (!string.IsNullOrEmpty(sessionCheckResult.ExceptionString))
            {
                return StatusCode((int)HttpStatusCode.Forbidden);
            }

            var res =
                await _ganjoorService.SuggestSong(userId, song);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get next unreviewed song
        /// </summary>
        /// <param name="skip"></param>
        /// <param name="onlyMine"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("song")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(PoemMusicTrackViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetNextUnreviewedSong(int skip = 0, bool onlyMine = false)
        {
            Guid userId =
                 new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            Guid sessionId =
                new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value);
            RServiceResult<bool> sessionCheckResult = await _appUserService.SessionExists(userId, sessionId);
            if (!string.IsNullOrEmpty(sessionCheckResult.ExceptionString))
            {
                return StatusCode((int)HttpStatusCode.Forbidden);
            }

            var res =
                await _ganjoorService.GetNextUnreviewedSong(skip, onlyMine ? userId : Guid.Empty);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);

            if (res.Result == null)
                return NotFound();

            var resCount = await _ganjoorService.GetUnreviewedSongsCount(onlyMine ? userId : Guid.Empty);
            if (!string.IsNullOrEmpty(resCount.ExceptionString))
                return BadRequest(resCount.ExceptionString);

            // Paging Header
            HttpContext.Response.Headers.Add("paging-headers",
                JsonConvert.SerializeObject(
                    new PaginationMetadata()
                    {
                        totalCount = resCount.Result,
                        pageSize = -1,
                        currentPage = -1,
                        hasNextPage = false,
                        hasPreviousPage = false,
                        totalPages = -1
                    })
                );

            return Ok(res.Result);
        }

        /// <summary>
        /// review song
        /// </summary>
        /// <param name="song"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("song")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.ReviewSongs)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(PoemMusicTrackViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> ReviewSong([FromBody] PoemMusicTrackViewModel song)
        {
            var res =
                await _ganjoorService.ReviewSong(song);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// directly insert a poem related song
        /// </summary>
        /// <param name="song"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("song/add")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.AddSongs)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(PoemMusicTrackViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> DirectInsertSong([FromBody] PoemMusicTrackViewModel song)
        {
            var res =
                await _ganjoorService.DirectInsertSong(song);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        ///  get a random poem from hafez
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("hafez/faal")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPoemCompleteViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> Faal()
        {
            RServiceResult<GanjoorPoemCompleteViewModel> res =
                await _ganjoorService.Faal();
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get recent comments
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="filterUserId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("comments")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<GanjoorCommentFullViewModel>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]

        public async Task<IActionResult> GetRecentComments([FromQuery] PagingParameterModel paging, Guid? filterUserId = null)
        {
            var comments = await _ganjoorService.GetRecentComments(paging, filterUserId == null ? Guid.Empty : (Guid)filterUserId, true);
            if (!string.IsNullOrEmpty(comments.ExceptionString))
            {
                return BadRequest(comments.ExceptionString);
            }

            // Paging Header
            HttpContext.Response.Headers.Add("paging-headers", JsonConvert.SerializeObject(comments.Result.PagingMeta));

            return Ok(comments.Result.Items);
        }

        /// <summary>
        /// get logged on users recent comments
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>

        [HttpGet]
        [Route("comments/mine")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<GanjoorCommentFullViewModel>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]

        public async Task<IActionResult> GetMyRecentComments([FromQuery] PagingParameterModel paging)
        {
            Guid userId =
                 new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            Guid sessionId =
                new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value);
            RServiceResult<bool> sessionCheckResult = await _appUserService.SessionExists(userId, sessionId);
            if (!string.IsNullOrEmpty(sessionCheckResult.ExceptionString))
            {
                return StatusCode((int)HttpStatusCode.Forbidden);
            }

            var comments = await _ganjoorService.GetRecentComments(paging, userId, false);
            if (!string.IsNullOrEmpty(comments.ExceptionString))
            {
                return BadRequest(comments.ExceptionString);
            }

            // Paging Header
            HttpContext.Response.Headers.Add("paging-headers", JsonConvert.SerializeObject(comments.Result.PagingMeta));

            return Ok(comments.Result.Items);
        }



        /// <summary>
        /// post new comment
        /// </summary>
        /// <param name="comment"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("comment")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorCommentSummaryViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> NewComment([FromBody] GanjoorCommentPostViewModel comment)
        {
            string clientIPAddress = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();

            Guid userId =
                 new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            Guid sessionId =
                new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value);
            RServiceResult<bool> sessionCheckResult = await _appUserService.SessionExists(userId, sessionId);
            if (!string.IsNullOrEmpty(sessionCheckResult.ExceptionString))
            {
                return StatusCode((int)HttpStatusCode.Forbidden);
            }

            var res =
                await _ganjoorService.NewComment(userId, clientIPAddress, comment.PoemId, comment.HtmlComment, comment.InReplyToId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// edit user's own comment
        /// </summary>
        /// <param name="id"></param>
        /// <param name="comment"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("comment/{id}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> EditMyComment(int id, [FromBody] string comment)
        {

            Guid userId =
                 new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            Guid sessionId =
                new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value);
            RServiceResult<bool> sessionCheckResult = await _appUserService.SessionExists(userId, sessionId);
            if (!string.IsNullOrEmpty(sessionCheckResult.ExceptionString))
            {
                return StatusCode((int)HttpStatusCode.Forbidden);
            }

            var res =
                await _ganjoorService.EditMyComment(userId, id, comment);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            if (!res.Result)
                return NotFound();
            return Ok();
        }

        /// <summary>
        /// delete user's own comment
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("comment")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> DeleteMyComment(int id)
        {

            Guid userId =
                 new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            Guid sessionId =
                new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value);
            RServiceResult<bool> sessionCheckResult = await _appUserService.SessionExists(userId, sessionId);
            if (!string.IsNullOrEmpty(sessionCheckResult.ExceptionString))
            {
                return StatusCode((int)HttpStatusCode.Forbidden);
            }

            var res =
                await _ganjoorService.DeleteMyComment(userId, id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            if (!res.Result)
                return NotFound();
            return Ok();
        }

        /// <summary>
        /// report a comment
        /// </summary>
        /// <param name="report"></param>
        /// <returns>id of reported record</returns>
        [HttpPost]
        [Route("comment/report")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(int))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> ReportComment([FromBody] GanjoorPostReportCommentViewModel report)
        {
            Guid userId =
                 new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            Guid sessionId =
                new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value);
            RServiceResult<bool> sessionCheckResult = await _appUserService.SessionExists(userId, sessionId);
            if (!string.IsNullOrEmpty(sessionCheckResult.ExceptionString))
            {
                return StatusCode((int)HttpStatusCode.Forbidden);
            }

            RServiceResult<int> res = await _ganjoorService.ReportComment(userId, report);
            if (!string.IsNullOrEmpty(res.ExceptionString))
            {
                return BadRequest(res.ExceptionString);
            }
            return Ok(res.Result);
        }

        /// <summary>
        /// delete a report (without deleting corresponding comment)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("comment/report/{id}")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.ModerateOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(int))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> DeleteReport(int id)
        {
            RServiceResult<bool> res = await _ganjoorService.DeleteReport(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
            {
                return BadRequest(res.ExceptionString);
            }
            if(!res.Result)
            {
                return NotFound();
            }
            return Ok();
        }


        /// <summary>
        /// get list of reported comments
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("comments/reported")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.ModerateOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<GanjoorCommentAbuseReportViewModel>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetReportedComments([FromQuery] PagingParameterModel paging)
        {
            var comments = await _ganjoorService.GetReportedComments(paging);
            if (!string.IsNullOrEmpty(comments.ExceptionString))
            {
                return BadRequest(comments.ExceptionString);
            }

            // Paging Header
            HttpContext.Response.Headers.Add("paging-headers", JsonConvert.SerializeObject(comments.Result.PagingMeta));

            return Ok(comments.Result.Items);
        }

        /// <summary>
        /// delete other users comment comment
        /// </summary>
        /// <param name="id"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("comment/moderate/{id}")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.ModerateOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> DeleteModerateComment(int id, [FromBody]string reason)
        {

            var res =
                await _ganjoorService.DeleteModerateComment(id, reason);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            if (!res.Result)
                return NotFound();
            return Ok();
        }

        /// <summary>
        /// imports data from ganjoor SQLite database
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("sqliteimport")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.ImportOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> ImportLocalSQLiteDb()
        {
            RServiceResult<bool> res =
                await _ganjoorService.ImportLocalSQLiteDb();
            if (res.Result)
                return Ok();
            return BadRequest(res.ExceptionString);
        }

        /// <summary>
        /// imports data from ganjoor MySql database
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("mysqlimport")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.ImportOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public IActionResult ImportFromMySql()
        {
            RServiceResult<bool> res =
                 _ganjoorService.ImportFromMySql();
            if (res.Result)
                return Ok();
            return BadRequest(res.ExceptionString);
        }

        /// <summary>
        /// Get user public profile
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("user/profile/{id}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorUserPublicProfile))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetUserPublicProfile(Guid id)
        {
            RServiceResult<PublicRAppUser> userInfo = await _appUserService.GetUserInformation(id);
            if (userInfo.Result == null)
            {
                if (string.IsNullOrEmpty(userInfo.ExceptionString))
                    return NotFound();
                return BadRequest(userInfo.ExceptionString);
            }
            return Ok
                (
                new GanjoorUserPublicProfile()
                {
                    Id = id,
                    NickName = userInfo.Result.NickName,
                    Bio = userInfo.Result.Bio,
                    Website = userInfo.Result.Website,
                    RImageId = userInfo.Result.RImageId
                }
                );
        }


        /// <summary>
        ///  Get Verses By query
        /// </summary>
        /// <param name="query"></param>
        /// <param name="poetId"></param>
        /// <param name="paging"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("verse/search")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<GanjoorSearchVerseViewModel>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]

        public async Task<IActionResult> GetVersesByQuery(string query, int poetId, [FromQuery] PagingParameterModel paging)
        {
            var pagedResult = await _ganjoorService.GetVersesByQuery(query, poetId, paging);
            if (!string.IsNullOrEmpty(pagedResult.ExceptionString))
                return BadRequest(pagedResult.ExceptionString);

            // Paging Header
            HttpContext.Response.Headers.Add("paging-headers", JsonConvert.SerializeObject(pagedResult.Result.PagingMeta));

            return Ok(pagedResult.Result.items);
        }

        /// <summary>
        /// Get Similar Poems accroding to prosody and rhyme informations
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="metre">cannot be empty</param>
        /// <param name="rhyme">can be empty</param>
        /// <param name="poetId">send 0 for all</param>
        /// <returns>return value is not complete or valid for some parts, you should use only the valid parts!</returns>

        [HttpGet]
        [Route("poems/similar")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<GanjoorPoemCompleteViewModel>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]

        public async Task<IActionResult> GetSimilarPoems([FromQuery]PagingParameterModel paging, string metre, string rhyme, int poetId = 0)
        {
            var pagedResult = await _ganjoorService.GetSimilarPoems(paging, metre, rhyme, poetId == 0 ? (int?) null : poetId);
            if (!string.IsNullOrEmpty(pagedResult.ExceptionString))
                return BadRequest(pagedResult.ExceptionString);

            // Paging Header
            HttpContext.Response.Headers.Add("paging-headers", JsonConvert.SerializeObject(pagedResult.Result.PagingMeta));

            return Ok(pagedResult.Result.Items);
        }

        /// <summary>
        /// search
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="term"></param>
        /// <param name="poetId"></param>
        /// <param name="catId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("poems/search")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<GanjoorPoemCompleteViewModel>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]

        public async Task<IActionResult> Search([FromQuery] PagingParameterModel paging, string term, int poetId = 0, int catId = 0)
        {
            var pagedResult = await _ganjoorService.Search(paging, term, poetId == 0 ? (int?)null : poetId, catId == 0 ? (int?)null: catId);
            if (!string.IsNullOrEmpty(pagedResult.ExceptionString))
                return BadRequest(pagedResult.ExceptionString);

            // Paging Header
            HttpContext.Response.Headers.Add("paging-headers", JsonConvert.SerializeObject(pagedResult.Result.PagingMeta));

            return Ok(pagedResult.Result.Items);
        }

        /// <summary>
        /// returns ganjoor metre list ordered by rhythm
        /// </summary>
        /// <returns></returns>

        [HttpGet]
        [Route("rhythms")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<GanjoorMetre>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]

        public async Task<IActionResult> GetGanjoorMetres()
        {
            var res = await _ganjoorService.GetGanjoorMetres();

            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);

            return Ok(res.Result);
        }

        /// <summary>
        /// add site banner (send form) with these fields: alt, url and an image attachment
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("site/banner")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.Banners)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorSiteBannerViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> AddSiteBanner()
        {
            try
            {
                if (!Request.Form.TryGetValue("alt", out Microsoft.Extensions.Primitives.StringValues alt))
                {
                    return BadRequest("alt is null");
                }
                if (!Request.Form.TryGetValue("url", out Microsoft.Extensions.Primitives.StringValues url))
                {
                    return BadRequest("url is null");
                }
                if(Request.Form.Files.Count != 1)
                {
                    return BadRequest("a single image is not provided");
                }
                using Stream stream = Request.Form.Files[0].OpenReadStream();
                RServiceResult<GanjoorSiteBannerViewModel> res = await _ganjoorService.AddSiteBanner(stream, Request.Form.Files[0].FileName, alt.ToString(), url.ToString(), false);
                if (!string.IsNullOrEmpty(res.ExceptionString))
                {
                    return BadRequest(res.ExceptionString);
                }
                return Ok(res.Result);
            }
            catch (Exception exp)
            {
                return BadRequest(exp.ToString());
            }
        }

        /// <summary>
        /// modify site banner
        /// </summary>
        /// <param name="id"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("site/banner/{id}")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.Banners)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> ModifySiteBanner(int id, [FromBody] GanjoorSiteBannerModifyViewModel model)
        {
            try
            {

                RServiceResult<bool> res = await _ganjoorService.ModifySiteBanner(id, model.AlternateText, model.TargetUrl, model.Active);

                if (!string.IsNullOrEmpty(res.ExceptionString))
                {
                    return BadRequest(res.ExceptionString);
                }

                if(!res.Result)
                {
                    return NotFound();
                }

                return Ok();
            }
            catch (Exception exp)
            {
                return BadRequest(exp.ToString());
            }
        }

        /// <summary>
        /// delete site banner
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("site/banner")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.Banners)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteSiteBanner(int id)
        {
            try
            {

                RServiceResult<bool> res = await _ganjoorService.DeleteSiteBanner(id);

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
            catch (Exception exp)
            {
                return BadRequest(exp.ToString());
            }
        }

        /// <summary>
        /// get all defined site banners
        /// </summary>
        /// <returns></returns>

        [HttpGet]
        [Route("site/banners")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.Banners)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorSiteBannerViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetSiteBanners()
        {
            try
            {
                RServiceResult<GanjoorSiteBannerViewModel[]> res = await _ganjoorService.GetSiteBanners();

                if (!string.IsNullOrEmpty(res.ExceptionString))
                {
                    return BadRequest(res.ExceptionString);
                }
                return Ok(res.Result);
            }
            catch (Exception exp)
            {
                return BadRequest(exp.ToString());
            }
        }

        /// <summary>
        /// get a random active site banner
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("site/banner")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorSiteBannerViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetARandomActiveSiteBanner()
        {
            try
            {
                RServiceResult<GanjoorSiteBannerViewModel> res = await _ganjoorService.GetARandomActiveSiteBanner();

                if (!string.IsNullOrEmpty(res.ExceptionString))
                {
                    return BadRequest(res.ExceptionString);
                }

                return Ok(res.Result); //this might be null
            }
            catch (Exception exp)
            {
                return BadRequest(exp.ToString());
            }
        }

        /// <summary>
        /// examine site pages for broken links
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("healthcheck")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.ImportOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public IActionResult HealthCheckContents()
        {
            RServiceResult<bool> res =
                 _ganjoorService.HealthCheckContents();
            if (res.Result)
                return Ok();
            return BadRequest(res.ExceptionString);
        }

        /// <summary>
        /// Ganjoor Service
        /// </summary>

        protected readonly IGanjoorService _ganjoorService;

        /// <summary>
        /// IAppUserService instance
        /// </summary>
        protected IAppUserService _appUserService;

        /// <summary>
        /// for client IP resolution
        /// </summary>
        protected IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Image Service
        /// </summary>
        protected readonly IImageFileService _imageFileService;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="ganjoorService"></param>
        /// <param name="appUserService"></param>
        /// <param name="httpContextAccessor"></param>
        /// <param name="imageFileService"></param>
        public GanjoorController(IGanjoorService ganjoorService, IAppUserService appUserService, IHttpContextAccessor httpContextAccessor, IImageFileService imageFileService)
        {
            _ganjoorService = ganjoorService;
            _appUserService = appUserService;
            _httpContextAccessor = httpContextAccessor;
            _imageFileService = imageFileService;
        }
    }
}
