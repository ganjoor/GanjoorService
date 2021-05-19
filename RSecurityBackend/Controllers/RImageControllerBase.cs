using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Models.Image;
using RSecurityBackend.Services;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace RSecurityBackend.Controllers
{
    /// <summary>
    /// Generic Image Provider
    /// </summary>
    [Produces("application/json")]
    [Route("api/rimages")]
    public abstract class RImageControllerBase : Controller
    {
        /// <summary>
        /// returns image stream
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}.jpg")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(FileStreamResult))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> Get(Guid id)
        {
            var cachKey = $"RImageControllerBase::RImage::{id}";
            if(!_memoryCache.TryGetValue(cachKey, out RImage img))
            {
                RServiceResult<RImage> imgRes =
               await _pictureFileService.GetImage(id);

                if (!string.IsNullOrEmpty(imgRes.ExceptionString))
                    return BadRequest(imgRes.ExceptionString);

                if (imgRes.Result == null)
                    return NotFound();

                img = imgRes.Result;

                _memoryCache.Set(cachKey, img);
            }

            var cachKeyForPath = $"RImageControllerBase::Path::{id}";
            if (!_memoryCache.TryGetValue(cachKeyForPath, out string imgPath))
            {
                RServiceResult<string> imgPathRes = _pictureFileService.GetImagePath(img);
                if (!string.IsNullOrEmpty(imgPathRes.ExceptionString))
                    return BadRequest(imgPathRes.ExceptionString);

                imgPath = imgPathRes.Result;

                _memoryCache.Set(cachKeyForPath, imgPath);
            }

            Response.GetTypedHeaders().LastModified = img.LastModified;

            var requestHeaders = Request.GetTypedHeaders();
            if (requestHeaders.IfModifiedSince.HasValue &&
                requestHeaders.IfModifiedSince.Value >= img.LastModified)
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            return new FileStreamResult(new FileStream(imgPath, FileMode.Open, FileAccess.Read), "image/jpeg");

        }

        /// <summary>
        /// temporary api for uploading temporary images
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Policy = SecurableItem.AuditLogEntityShortName + ":" + SecurableItem.ViewOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(Guid))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden, Type = typeof(string))]
        public async Task<IActionResult> UploadImage()
        {
            try
            {
                if (!Request.Form.TryGetValue("path", out Microsoft.Extensions.Primitives.StringValues path))
                {
                    return BadRequest("path is null");
                }

                IFormFile file = Request.Form.Files[0];

                RServiceResult<RImage> image = await _pictureFileService.Add(file, null, file.FileName, path);


                if (!string.IsNullOrEmpty(image.ExceptionString))
                {
                    return BadRequest(image.ExceptionString);
                }

                image = await _pictureFileService.Store(image.Result);

                if (!string.IsNullOrEmpty(image.ExceptionString))
                {
                    return BadRequest(image.ExceptionString);
                }

                return Ok(image.Result.Id);
            }
            catch (Exception exp)
            {
                return BadRequest(exp.ToString());
            }
        }


        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="pictureFileService"></param>
        /// <param name="memoryCache"></param>
        public RImageControllerBase(IImageFileService pictureFileService, IMemoryCache memoryCache)
        {
            _pictureFileService = pictureFileService;
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// Artifact Service
        /// </summary>
        protected readonly IImageFileService _pictureFileService;

        /// <summary>
        /// IMemoryCache
        /// </summary>
        protected readonly IMemoryCache _memoryCache;


    }
}
