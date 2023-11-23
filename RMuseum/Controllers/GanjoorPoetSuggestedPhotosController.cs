using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RMuseum.Models.Auth.Memory;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Services;
using RSecurityBackend.Models.Generic;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace RMuseum.Controllers
{
    [Produces("application/json")]
    [Route("api/poetphotos")]
    public class GanjoorPoetSuggestedPhotosController : Controller
    {
        /// <summary>
        /// returns list of suggested photos for a poet
        /// </summary>
        /// <param name="id">poet id</param>
        /// <returns></returns>
        [HttpGet("poet/{id}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPoetSuggestedPictureViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetPoetSuggestedPhotosAsync(int id)
        {
            var res = await _poetPhotosService.GetPoetSuggestedPhotosAsync(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// returns a single suggested photo
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.ModeratePoetPhotos)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPoetSuggestedPictureViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetPoetSuggestedPhotoByIdAsync(int id)
        {
            var res = await _poetPhotosService.GetPoetSuggestedPhotoByIdAsync(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// return the next unpublished suggested line for poets
        /// </summary>
        /// <param name="skip"></param>
        /// <returns></returns>

        [HttpGet("unpublished/next")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.ModeratePoetPhotos)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPoetSuggestedPictureViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetNextUnmoderatedPoetSuggestedPhotoAsync(int skip)
        {
            var res = await _poetPhotosService.GetNextUnmoderatedPoetSuggestedPhotoAsync(skip);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            if (res.Result == null)
                return NotFound();

            var resCount = await _poetPhotosService.GetNextUnmoderatedPoetSuggestedPhotosCountAsync();
            if (!string.IsNullOrEmpty(resCount.ExceptionString))
                return BadRequest(resCount.ExceptionString);

            // Paging Header
            HttpContext.Response.Headers.Append("paging-headers",
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
        /// add a suggestion for poets photos (send form) with these fields: poetId, title, description, srcUrl and an image attachment
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPoetSuggestedPictureViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> SuggestPhotoForPoet()
        {
            try
            {
                if (!Request.Form.TryGetValue("poetId", out Microsoft.Extensions.Primitives.StringValues poetId))
                {
                    return BadRequest("poetId is null");
                }
                if (!Request.Form.TryGetValue("title", out Microsoft.Extensions.Primitives.StringValues title))
                {
                    return BadRequest("title is null");
                }
                if (!Request.Form.TryGetValue("description", out Microsoft.Extensions.Primitives.StringValues description))
                {
                    return BadRequest("description is null");
                }
                if (!Request.Form.TryGetValue("srcUrl", out Microsoft.Extensions.Primitives.StringValues srcUrl))
                {
                    return BadRequest("srcUrl is null");
                }
                if (Request.Form.Files.Count != 1)
                {
                    return BadRequest("a single image is not provided");
                }
                Guid userId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
                using Stream stream = Request.Form.Files[0].OpenReadStream();
                RServiceResult<GanjoorPoetSuggestedPictureViewModel> res = await _poetPhotosService.SuggestPhotoForPoet(int.Parse(poetId.ToString()), userId, stream, Request.Form.Files[0].FileName, title.ToString(), description.ToString(), srcUrl.ToString());
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
        /// modify a suggestion for poets photos
        /// </summary>
        /// <param name="photo"></param>
        /// <returns></returns>
        [HttpPut]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.ModeratePoetPhotos)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> ModifyPoetSuggestedPhotosAsync([FromBody] GanjoorPoetSuggestedPictureViewModel photo)
        {
            var res = await _poetPhotosService.ModifyPoetSuggestedPhotoAsync(photo);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// reject  a suggestion for poets photos
        /// </summary>
        /// <param name="id"></param>
        /// <param name="rejectionCause"></param>
        /// <returns></returns>
        [HttpPut("reject/{id}")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.ModeratePoetPhotos)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> RejectPoetSuggestedPhotosAsync(int id, [FromBody] string rejectionCause)
        {
            Guid userId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            var res = await _poetPhotosService.RejectPoetSuggestedPhotosAsync(id, userId, rejectionCause);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// delete published suggested photo
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.ModeratePoetPhotos)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> DeletePoetSuggestedPhotoAsync(int id)
        {

            var res = await _poetPhotosService.DeletePoetSuggestedPhotoAsync(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// Poets Photos Service
        /// </summary>
        protected readonly IPoetPhotoSuggestionService _poetPhotosService;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="ganjoorService"></param>
        public GanjoorPoetSuggestedPhotosController(IPoetPhotoSuggestionService ganjoorService)
        {
            _poetPhotosService = ganjoorService;
        }
    }
}
