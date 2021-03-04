using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RMuseum.Models.Auth.Memory;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Models.GanjoorAudio.ViewModels;
using RMuseum.Models.GanjoorIntegration.ViewModels;
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
        public async Task<IActionResult> ReviewSong([FromBody]PoemMusicTrackViewModel song)
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
        /// <returns></returns>
        [HttpGet]
        [Route("comments")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<GanjoorCommentFullViewModel>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]

        public async Task<IActionResult> GetRecentComments([FromQuery] PagingParameterModel paging)
        {
            var comments = await _ganjoorService.GetRecentComments(paging);
            if (!string.IsNullOrEmpty(comments.ExceptionString))
            {
                return BadRequest(comments.ExceptionString);
            }

            // Paging Header
            HttpContext.Response.Headers.Add("paging-headers", JsonConvert.SerializeObject(comments.Result.PagingMeta));

            return Ok(comments.Result.Items);
        }

        /// <summary>
        /// get list of reported comments
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("comments/reported")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.ModerateOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<GanjoorCommentFullViewModel>))]
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
        public async Task<IActionResult> EditMyComment(int id, [FromBody]string comment)
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
        ///  Search Verses By query
        /// </summary>
        /// <param name="query"></param>
        /// <param name="poetId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("verse/search")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorSearchVerseViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> SearchVersesByQuery(string query, int poetId = 0)
        {
            RServiceResult<GanjoorSearchVerseViewModel[]> res = await _ganjoorService.SearchVersesByQuery(query, poetId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            if (res.Result == null)
                return NotFound();
            return Ok(res.Result);
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
        /// constructor
        /// </summary>
        /// <param name="ganjoorService"></param>
        /// <param name="appUserService"></param>
        /// <param name="httpContextAccessor"></param>
        public GanjoorController(IGanjoorService ganjoorService, IAppUserService appUserService, IHttpContextAccessor httpContextAccessor)
        {
            _ganjoorService = ganjoorService;
            _appUserService = appUserService;
            _httpContextAccessor = httpContextAccessor;
        }
    }
}
