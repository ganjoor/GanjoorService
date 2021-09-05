using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RMuseum.Models.Auth.Memory;
using RMuseum.Models.Auth.ViewModel;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Models.GanjoorAudio.ViewModels;
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
        /// get list of published poets without their biography
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("poets")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPoetViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetPoets()
        {
            var cacheKey = $"ganjoor/poets";
            if (!_memoryCache.TryGetValue(cacheKey, out GanjoorPoetViewModel[] poets))
            {
                RServiceResult<GanjoorPoetViewModel[]>  res =
                await _ganjoorService.GetPoets(true, false);
                if (!string.IsNullOrEmpty(res.ExceptionString))
                    return BadRequest(res.ExceptionString);

                poets = res.Result;
                _memoryCache.Set(cacheKey, poets);
                
            }
            return Ok(poets);
        }


        /// <summary>
        /// get list of all poets (including unpublished ones) with their bio
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("poets/secure")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPoetViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetAllPoets()
        {
            RServiceResult<GanjoorPoetViewModel[]> res =
                 await _ganjoorService.GetPoets(false, true);
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
            var cacheKey = $"poet/byid/{id}";
            if (!_memoryCache.TryGetValue(cacheKey, out GanjoorPoetCompleteViewModel poet))
            {
                RServiceResult<GanjoorPoetCompleteViewModel> res =
                await _ganjoorService.GetPoetById(id);
                if (!string.IsNullOrEmpty(res.ExceptionString))
                    return BadRequest(res.ExceptionString);
                if (res.Result == null)
                    return NotFound();
                poet = res.Result;
                _memoryCache.Set(cacheKey, poet);
            }
            return Ok(poet);
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
            var cacheKey = $"poet/byurl/{url}";
            if (!_memoryCache.TryGetValue(cacheKey, out GanjoorPoetCompleteViewModel poet))
            {
                RServiceResult<GanjoorPoetCompleteViewModel> res =
                 await _ganjoorService.GetPoetByUrl(url);
                if (!string.IsNullOrEmpty(res.ExceptionString))
                    return BadRequest(res.ExceptionString);
                if (res.Result == null)
                    return NotFound();
                poet = res.Result;
                _memoryCache.Set(cacheKey, poet);
            }
            return Ok(poet);
        }

        /// <summary>
        /// update poet info (except for image)
        /// </summary>
        /// <param name="id"></param>
        /// <param name="poet"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("poet/{id}")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> ModifyPoet(int id, [FromBody]GanjoorPoetViewModel poet)
        {

            if (!string.IsNullOrEmpty(poet.ImageUrl))
            {
                return BadRequest("Please send an empty image url, if you are trying to change poet image this is not the right method to do it.");
            }

            Guid userId =
             new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

            poet.Id = id;

            var res = await _ganjoorService.UpdatePoetAsync(poet, userId);

            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);

            return Ok(res.Result);
        }

        /// <summary>
        /// create new poet
        /// </summary>
        /// <param name="poet"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("poet")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPoetCompleteViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> CreatePoet([FromBody] GanjoorPoetViewModel poet)
        {

            if (!string.IsNullOrEmpty(poet.ImageUrl))
            {
                return BadRequest("Please send an empty image url, if you are trying to change poet image this is not the right method to do it.");
            }

            Guid userId =
             new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

            var res = await _ganjoorService.AddPoetAsync(poet, userId);

            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);

            return Ok(res.Result);
        }

        /// <summary>
        /// starts deleting poet job
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("poet/{id}")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public IActionResult StartDeletePoet(int id)
        {
            var res = _ganjoorService.StartDeletePoet(id);

            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);

            return Ok();
        }

        /// <summary>
        /// get poet image with png ext
        /// </summary>
        /// <param name="url">sample: hafez</param>
        /// <returns></returns>
        [HttpGet("poet/image/{url}.png")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(FileStreamResult))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetPoetImageInPng(string url)
        {
            return await GetPoetImage(url.Replace(".png", ".gif"));
        }

        /// <summary>
        /// get poet image
        /// </summary>
        /// <param name="url">sample: hafez</param>
        /// <returns></returns>
        [HttpGet("poet/image/{url}.gif")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(FileStreamResult))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetPoetImage(string url)
        {

            var cacheKey = $"poet/image/{url}.gif";
            var cacheKeyForLastModified = $"{cacheKey}/lastModified";
            if (!_memoryCache.TryGetValue(cacheKey, out string imagePath) || !_memoryCache.TryGetValue(cacheKeyForLastModified, out DateTime lastModified))
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

                lastModified = img.Result.LastModified;

                

                RServiceResult<string> imgPath = _imageFileService.GetImagePath(img.Result);
                if (!string.IsNullOrEmpty(imgPath.ExceptionString))
                    return BadRequest(imgPath.ExceptionString);

                imagePath = imgPath.Result;


                _memoryCache.Set(cacheKey, imagePath);
                _memoryCache.Set(cacheKeyForLastModified, lastModified);

            }

            Response.GetTypedHeaders().LastModified = lastModified;

            var requestHeaders = Request.GetTypedHeaders();
            if (requestHeaders.IfModifiedSince.HasValue &&
                requestHeaders.IfModifiedSince.Value >= lastModified)
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            return new FileStreamResult(new FileStream(imagePath, FileMode.Open, FileAccess.Read), "image/gif");

        }

        /// <summary>
        /// set poet image
        /// </summary>
        /// <param name="id">poet image</param>
        /// <returns></returns>
        [HttpPost("poet/image/{id}")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden, Type = typeof(string))]
        public async Task<IActionResult> UploadPoetImage(int id)
        {
            try
            {
                var poet = await _ganjoorService.GetPoetById(id);
                IFormFile file = Request.Form.Files[0];
                RServiceResult<RImage> image = await _imageFileService.Add(file, null, file.FileName, Path.Combine(Configuration.GetSection("PictureFileService")["StoragePath"], "PoetImages"));
                if (!string.IsNullOrEmpty(image.ExceptionString))
                {
                    return BadRequest(image.ExceptionString);
                }
                image = await _imageFileService.Store(image.Result);
                if (!string.IsNullOrEmpty(image.ExceptionString))
                {
                    return BadRequest(image.ExceptionString);
                }

                var res = await _ganjoorService.ChangePoetImageAsync(id, image.Result.Id);

                if (!string.IsNullOrEmpty(res.ExceptionString))
                    return BadRequest(res.ExceptionString);

                if(res.Result)
                {
                    var cacheKey = $"poet/image/{poet.Result.Cat.UrlSlug}.png";
                    if (_memoryCache.TryGetValue(cacheKey, out string imagePath))
                    {
                        _memoryCache.Remove(cacheKey);
                    }
                }

                return Ok(res.Result);
            }
            catch (Exception exp)
            {
                return BadRequest(exp.ToString());
            }
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
            var cacheKey = $"cat/byid/{id}/{poems}";
            if (!_memoryCache.TryGetValue(cacheKey, out GanjoorPoetCompleteViewModel cat))
            {
                RServiceResult<GanjoorPoetCompleteViewModel> res =
               await _ganjoorService.GetCatById(id, poems);
                if (!string.IsNullOrEmpty(res.ExceptionString))
                    return BadRequest(res.ExceptionString);
                if (res.Result == null)
                    return NotFound();
                cat = res.Result;
                _memoryCache.Set(cacheKey, cat);
            }
            return Ok(cat);
        }

        /// <summary>
        /// cat by full url
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
            var cacheKey = $"cat/byurl/{url}/{poems}";
            if (!_memoryCache.TryGetValue(cacheKey, out GanjoorPoetCompleteViewModel cat))
            {
                RServiceResult<GanjoorPoetCompleteViewModel> res =
                 await _ganjoorService.GetCatByUrl(url, poems);
                if (!string.IsNullOrEmpty(res.ExceptionString))
                    return BadRequest(res.ExceptionString);
                if (res.Result == null)
                    return NotFound();
                cat = res.Result;
                _memoryCache.Set(cacheKey, cat);
            }

            
            return Ok(cat);
        }

        /// <summary>
        /// batch rename cat poems
        /// </summary>
        /// <param name="id"></param>
        /// <param name="model"></param>
        /// <returns></returns>

        [HttpPut]
        [Route("cat/recaptionpoems/{id}")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPageCompleteViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> BatchRenameCatPoemTitles(int id, [FromBody]GanjoorBatchNamingModel model)
        {
            Guid userId =
               new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

            RServiceResult<string[]> res =
                await _ganjoorService.BatchRenameCatPoemTitles(id, model, userId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// starts assigning poem rhymes
        /// </summary>
        /// <param name="id"></param>
        /// <param name="retag"></param>
        /// <returns></returns>

        [HttpPut]
        [Route("cat/startassigningrhymes/{id}/{retag}")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public  IActionResult FindCategoryPoemsRhymes(int id, bool retag)
        {

            RServiceResult<bool> res =
                _ganjoorService.FindCategoryPoemsRhymes(id, retag);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// start assigning poem rhuthmes
        /// </summary>
        /// <param name="id"></param>
        /// <param name="retag"></param>
        /// <param name="rhythm"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("cat/startassigningrhythms/{id}/{retag}")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public IActionResult FindCategoryPoemsRhythms(int id, bool retag, string rhythm = "")
        {

            RServiceResult<bool> res =
                _ganjoorService.FindCategoryPoemsRhythms(id, retag, rhythm);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// generate category toc
        /// </summary>
        /// <param name="id"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("cat/toc/{id}/{options}")]
        [Produces("text/plain")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GenerateTableOfContents(int id, GanjoorTOC options = GanjoorTOC.Analyse)
        {
            var res = await _ganjoorService.GenerateTableOfContents(id, options);
            if(!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// start generating sub cats TOC
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>

        [HttpPut("cat/subcats/startgentoc/{id}")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public IActionResult StartGeneratingSubCatsTOC(int id)
        {
            Guid userId =
               new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            var res = _ganjoorService.StartGeneratingSubCatsTOC(userId, id);

            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);

            return Ok();
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
                await _ganjoorService.UpdatePageAsync(id, userId, page);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            if (res.Result == null)
                return NotFound();
            return Ok(res.Result);
        }

       
        /// <summary>
        /// clean cache by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("page/cache/{id}")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPageCompleteViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> CacheCleanForPageById(int id)
        {
            await _ganjoorService.CacheCleanForPageById(id);
            return Ok();
        }


        /// <summary>
        /// delete page
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("page/{id}")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPageCompleteViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeletePageAsync(int id)
        {
            var res = await _ganjoorService.DeletePageAsync(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok();
        }

        /// <summary>
        /// older versions of a page (modifications history except for current version)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("page/oldversions/{id}")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPageSnapshotSummaryViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetOlderVersionsOfPage(int id)
        {
            RServiceResult<GanjoorPageSnapshotSummaryViewModel[]> res =
                await _ganjoorService.GetOlderVersionsOfPage(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }
        
        /// <summary>
        /// get old version of page
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("oldversion/{id}")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorModifyPageViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetOldVersionOfPage(int id)
        {
            RServiceResult<GanjoorModifyPageViewModel> res =
                await _ganjoorService.GetOldVersionOfPage(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
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
        /// send poem corrections
        /// </summary>
        /// <param name="correction"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("poem/correction")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPoemCorrectionViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> SuggestPoemCorrection([FromBody] GanjoorPoemCorrectionViewModel correction)
        {
            if (ReadOnlyMode)
                return BadRequest("سایت به دلایل فنی مثل انتقال سرور موقتاً در حالت فقط خواندنی قرار دارد. لطفاً ساعاتی دیگر مجدداً تلاش کنید.");

            correction.UserId =
               new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

            RServiceResult<GanjoorPoemCorrectionViewModel> res =
                await _ganjoorService.SuggestPoemCorrection(correction);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            if (res.Result == null)
                return NotFound();
            return Ok(res.Result);
        }

        /// <summary>
        /// delete unreviewed user corrections for a poem
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("poem/correction/{id}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPoemCorrectionViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeletePoemCorrections(int id)
        {
            if (ReadOnlyMode)
                return BadRequest("سایت به دلایل فنی مثل انتقال سرور موقتاً در حالت فقط خواندنی قرار دارد. لطفاً ساعاتی دیگر مجدداً تلاش کنید.");

            var userId =
               new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

            var res =
                await _ganjoorService.DeletePoemCorrections(userId, id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok();
        }

        /// <summary>
        /// returns last unreviewed correction from the user for a poem
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("poem/correction/last/{id}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPoemCorrectionViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetLastUnreviewedUserCorrectionForPoem(int id)
        {
            Guid userId =
               new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

            RServiceResult<GanjoorPoemCorrectionViewModel> res =
                await _ganjoorService.GetLastUnreviewedUserCorrectionForPoem(userId, id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);//might be null
        }

        /// <summary>
        /// get list of user suggested corrections
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("corrections/mine")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<GanjoorPoemCorrectionViewModel>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetUserCorrections([FromQuery] PagingParameterModel paging)
        {
            Guid userId =
               new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

            var res =
                await _ganjoorService.GetUserCorrections(userId, paging);

            // Paging Header
            HttpContext.Response.Headers.Add("paging-headers", JsonConvert.SerializeObject(res.Result.PagingMeta));

            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result.Items);
        }

        /// <summary>
        /// effective corrections for poem
        /// </summary>
        /// <param name="id"></param>
        /// <param name="paging"></param>
        /// <returns></returns>

        [HttpGet]
        [Route("poem/{id}/corrections/effective")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<GanjoorPoemCorrectionViewModel>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetPoemEffectiveCorrections(int id,[FromQuery] PagingParameterModel paging)
        {
            var res =
                await _ganjoorService.GetPoemEffectiveCorrections(id, paging);

            // Paging Header
            HttpContext.Response.Headers.Add("paging-headers", JsonConvert.SerializeObject(res.Result.PagingMeta));

            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result.Items);
        }

        /// <summary>
        /// get correction by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("correction/{id}")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPoemCorrectionViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetCorrectionById(int id)
        {
            Guid userId =
               new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

            RServiceResult<GanjoorPoemCorrectionViewModel> res =
                await _ganjoorService.GetCorrectionById(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);//might be null
        }

        /// <summary>
        /// get next unreviewed correction
        /// </summary>
        /// <param name="skip"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("poem/correction/next")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPoemCorrectionViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetNextUnreviewedCorrection(int skip = 0)
        {
            RServiceResult<GanjoorPoemCorrectionViewModel> res =
                await _ganjoorService.GetNextUnreviewedCorrection(skip);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);

            var resCount = await _ganjoorService.GetUnreviewedCorrectionCount();
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

            return Ok(res.Result);//might be null
        }

        /// <summary>
        /// moderate poem correction
        /// </summary>
        /// <param name="correction"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("correction/moderate")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPoemCorrectionViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> ModeratePoemCorrection([FromBody] GanjoorPoemCorrectionViewModel correction)
        {
            if (ReadOnlyMode)
                return BadRequest("سایت به دلایل فنی مثل انتقال سرور موقتاً در حالت فقط خواندنی قرار دارد. لطفاً ساعاتی دیگر مجدداً تلاش کنید.");

            Guid userId =
               new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

            RServiceResult<GanjoorPoemCorrectionViewModel> res =
                await _ganjoorService.ModeratePoemCorrection(userId, correction);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            if (res.Result == null)
                return NotFound();
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
            if (ReadOnlyMode)
                return BadRequest("سایت به دلایل فنی مثل انتقال سرور موقتاً در حالت فقط خواندنی قرار دارد. لطفاً ساعاتی دیگر مجدداً تلاش کنید.");
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
        /// history of suggested songs by a user
        /// </summary>
        [HttpGet]
        [Route("song/user/stats/{userId}")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.ReviewSongs)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(UserSongSuggestionsHistory))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> GetUserSongsSuggestionsStatistics(Guid userId)
        {
            var res =
                await _ganjoorService.GetUserSongsSuggestionsStatistics(userId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
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
            if (ReadOnlyMode)
                return BadRequest("سایت به دلایل فنی مثل انتقال سرور موقتاً در حالت فقط خواندنی قرار دارد. لطفاً ساعاتی دیگر مجدداً تلاش کنید.");

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
            if (ReadOnlyMode)
                return BadRequest("سایت به دلایل فنی مثل انتقال سرور موقتاً در حالت فقط خواندنی قرار دارد. لطفاً ساعاتی دیگر مجدداً تلاش کنید.");
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
                await _ganjoorService.Faal(2, true);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get a random poem from a poet (c.ganjoor.net replacement), 0 means random poet, 
        /// حافظ (2)، خیام (3)، ابوسعید ابوالخیر (26)، صائب (22)، سعدی (7)، باباطاهر (28)، مولوی (5)، اوحدی (19)، خواجو (20)، شهریار (35)، عراقی (21)، فروغی بسطامی (32)، سلمان ساوجی (40)، محتشم کاشانی (29)، امیرخسرو دهلوی (34)، سیف فرغانی (31)، عبید زاکانی (33)، هاتف اصفهانی (25) یا رهی معیری (41)
        /// </summary>
        /// <param name="poetId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("poem/random")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPoemCompleteViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetARandomPoem(int poetId)
        {
            RServiceResult<GanjoorPoemCompleteViewModel> res =
                await _ganjoorService.Faal(poetId, false);
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
            if (ReadOnlyMode)
                return BadRequest("سایت به دلایل فنی مثل انتقال سرور موقتاً در حالت فقط خواندنی قرار دارد. لطفاً ساعاتی دیگر مجدداً تلاش کنید.");

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
                await _ganjoorService.NewComment(userId, clientIPAddress, comment.PoemId, comment.HtmlComment, comment.InReplyToId, comment.CoupletIndex);
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
            if (ReadOnlyMode)
                return BadRequest("سایت به دلایل فنی مثل انتقال سرور موقتاً در حالت فقط خواندنی قرار دارد. لطفاً ساعاتی دیگر مجدداً تلاش کنید.");
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
            if (ReadOnlyMode)
                return BadRequest("سایت به دلایل فنی مثل انتقال سرور موقتاً در حالت فقط خواندنی قرار دارد. لطفاً ساعاتی دیگر مجدداً تلاش کنید.");
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
            if (ReadOnlyMode)
                return BadRequest("سایت به دلایل فنی مثل انتقال سرور موقتاً در حالت فقط خواندنی قرار دارد. لطفاً ساعاتی دیگر مجدداً تلاش کنید.");

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
            if (ReadOnlyMode)
                return BadRequest("سایت به دلایل فنی مثل انتقال سرور موقتاً در حالت فقط خواندنی قرار دارد. لطفاً ساعاتی دیگر مجدداً تلاش کنید.");
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
        /// delete reported other users comment comment
        /// </summary>
        /// <param name="reportid"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("comment/reported/moderate/{reportid}")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.ModerateOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> DeleteModerateComment(int reportid)
        {
            if (ReadOnlyMode)
                return BadRequest("سایت به دلایل فنی مثل انتقال سرور موقتاً در حالت فقط خواندنی قرار دارد. لطفاً ساعاتی دیگر مجدداً تلاش کنید.");
            var res =
                await _ganjoorService.DeleteModerateComment(reportid);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            if (!res.Result)
                return NotFound();
            return Ok();
        }

        /// <summary>
        /// imports data from ganjoor SQLite database (form file)
        /// </summary>
        /// <param name="poetId"></param>
        /// <returns></returns>

        [HttpPost]
        [Route("sqlite/import/{poetId}")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.ImportOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> ImportLocalSQLiteDb(int poetId)
        {
            IFormFile file = Request.Form.Files[0];

            RServiceResult<bool> res =
                await _ganjoorService.ImportFromSqlite(poetId, file);
            if (res.Result)
                return Ok();
            return BadRequest(res.ExceptionString);
        }

        /// <summary>
        /// Apply corrections from sqlite
        /// </summary>
        /// <param name="poetId"></param>
        /// <param name="note"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("sqlite/update/{poetId}")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.ImportOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> ApplyCorrectionsFromSqlite(int poetId, string note)
        {
            IFormFile file = Request.Form.Files[0];

            RServiceResult<bool> res =
                await _ganjoorService.ApplyCorrectionsFromSqlite(poetId, file, note);
            if (res.Result)
                return Ok();
            return BadRequest(res.ExceptionString);
        }

        /// <summary>
        /// export a poet to sqlite database
        /// </summary>
        /// <param name="poetId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("sqlite/export/{poetId}")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.ImportOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(FileStreamResult))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> ExportToSqlite(int poetId)
        {
            RServiceResult<string> res =
                await _ganjoorService.ExportToSqlite(poetId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return new FileStreamResult(new FileStream(res.Result, FileMode.Open, FileAccess.Read), "application/octet-stream");
        }

        /// <summary>
        /// start exporting all poets
        /// </summary>
        /// <returns></returns>
        [HttpPost("sqlite/batchexport")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.ReviewSongs)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public IActionResult StartBatchGenerateGDBFiles()
        {
            try
            {

                var res = _ganjoorService.StartBatchGenerateGDBFiles();
                if (!string.IsNullOrEmpty(res.ExceptionString))
                    return BadRequest(res.ExceptionString);
                return Ok();
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
        [Route("mysql/import")]
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
        /// find poem rhyme
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("poem/analysisrhyme/{id}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjooRhymeAnalysisResult))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]

        public async Task<IActionResult> FindPoemRhyme(int id)
        {
            var res = await _ganjoorService.FindPoemRhyme(id);

            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);

            return Ok(res.Result);
        }

        /// <summary>
        /// analysis poem to find its prosody information
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("poem/analysisrhythm/{id}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]

        public async Task<IActionResult> FindPoemRhythm(int id)
        {
            var res = await _ganjoorService.FindPoemRhythm(id);

            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);

            return Ok(res.Result);
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
        /// separate verses in poem.PlainText with  Environment.NewLine instead of SPACE
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("regenplaintext")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.ImportOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public IActionResult RegerneratePoemsPlainText()
        {
            RServiceResult<bool> res =
                 _ganjoorService.RegerneratePoemsPlainText();
            if (res.Result)
                return Ok();
            return BadRequest(res.ExceptionString);
        }

        /// <summary>
        /// start building sitemap
        /// </summary>
        /// <returns></returns>

        [HttpPost("sitemap")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public IActionResult StartBuildingSitemap()
        {
            var res = _ganjoorService.StartBuildingSitemap();

            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);

            return Ok();
        }

        /// <summary>
        /// regenerate stats page
        /// </summary>
        /// <returns></returns>
        [HttpPut("rebuild/stats")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public IActionResult StartUpdatingStatsPage()
        {
            try
            {
                Guid userId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

                var res = _ganjoorService.StartUpdatingStatsPage(userId);
                if (!string.IsNullOrEmpty(res.ExceptionString))
                    return BadRequest(res.ExceptionString);
                return Ok();
            }
            catch (Exception exp)
            {
                return BadRequest(exp.ToString());
            }
        }

        /// <summary>
        /// start updating mundex page
        /// </summary>
        /// <returns></returns>
        [HttpPut("rebuild/mundex")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.ReviewSongs)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public IActionResult StartUpdatingMundexPage()
        {
            try
            {
                Guid userId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

                var res = _ganjoorService.StartUpdatingMundexPage(userId);
                if (!string.IsNullOrEmpty(res.ExceptionString))
                    return BadRequest(res.ExceptionString);
                return Ok();
            }
            catch (Exception exp)
            {
                return BadRequest(exp.ToString());
            }
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
        /// IMemoryCache
        /// </summary>
        protected readonly IMemoryCache _memoryCache;

        /// <summary>
        /// Configuration
        /// </summary>
        protected IConfiguration Configuration { get; }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="ganjoorService"></param>
        /// <param name="appUserService"></param>
        /// <param name="httpContextAccessor"></param>
        /// <param name="imageFileService"></param>
        /// <param name="memoryCache"></param>
        /// <param name="configuration"></param>
        public GanjoorController(
            IGanjoorService ganjoorService, 
            IAppUserService appUserService, 
            IHttpContextAccessor httpContextAccessor, 
            IImageFileService imageFileService, 
            IMemoryCache memoryCache,
            IConfiguration configuration
            )
        {
            _ganjoorService = ganjoorService;
            _appUserService = appUserService;
            _httpContextAccessor = httpContextAccessor;
            _imageFileService = imageFileService;
            _memoryCache = memoryCache;
            Configuration = configuration;
        }
    }
}
