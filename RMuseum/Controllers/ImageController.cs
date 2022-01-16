using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RMuseum.Models.Artifact;
using RMuseum.Models.Auth.Memory;
using RMuseum.Services;
using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Models.Image;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace RMuseum.Controllers
{
    [Produces("application/json")]
    [Route("api/images")]
    public class ImageController : Controller
    {
        /// <summary>
        /// returns image stream with image/jpeg MIME type
        /// </summary>
        /// <param name="id"></param>
        /// <param name="size"></param>
        /// <param name="mimeForResized"></param>
        /// <returns></returns>
        [HttpGet("{size}/{id}.jpg")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(FileStreamResult))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        
        public async Task<IActionResult> Get(Guid id, string size, string mimeForResized = "image/jpeg")
        {
            RServiceResult<RPictureFile> img =
                await _pictureFileService.GetImage(id);

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

            RServiceResult<string> imgPath  = _pictureFileService.GetImagePath(img.Result, size);
            if(!string.IsNullOrEmpty(imgPath.ExceptionString))
                return BadRequest(imgPath.ExceptionString);

            if(string.IsNullOrEmpty(imgPath.Result))
            {
                if(size == "orig")
                {
                    imgPath = _pictureFileService.GetImagePath(img.Result, "norm");
                    if (!string.IsNullOrEmpty(imgPath.ExceptionString))
                        return BadRequest(imgPath.ExceptionString);
                }
                else
                {
                    return NotFound();
                }
                
            }

            return new FileStreamResult(new FileStream(imgPath.Result, FileMode.Open, FileAccess.Read),
                size == "orig" ?
                img.Result.ContentType
                :
                mimeForResized
                );
        }

        /// <summary>
        /// returns image stream with image/webp MIME type
        /// </summary>
        /// <param name="id"></param>
        /// <param name="size"></param>
        /// <returns></returns>

        [HttpGet("{size}/{id}.webp")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(FileStreamResult))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]

        public async Task<IActionResult> GetWebp(Guid id, string size)
        {
            return await Get(id, size, "image/webp");
        }

        /// <summary>
        /// Rotate Image in 90 deg. multiplicants: 90, 180 or 270
        /// </summary>
        /// <param name="id"></param>
        /// <param name="degIn90mul"></param>
        /// <returns></returns>
        [HttpPut("{id}/{degIn90mul}")]
        [Authorize(Policy = RMuseumSecurableItem.ArtifactEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RPictureFile))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> Rotate(Guid id, int degIn90mul)
        {
            RServiceResult<RPictureFile> res =  await _pictureFileService.RotateImage(id, degIn90mul);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }


        /// <summary>
        /// Generate Cropped Image Based On ThumbnailCoordinates For Notes
        /// </summary>
        /// <param name="id"></param>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns>id of cropped image</returns>
        [HttpGet("cropped/{id}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RImage))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GenerateCroppedImageBasedOnThumbnailCoordinates(Guid id, decimal left, decimal top, decimal width, decimal height)
        {
            RServiceResult<RImage> cropped = await _pictureFileService.GenerateCroppedImageBasedOnThumbnailCoordinates(id, left, top, width, height);
            if (!string.IsNullOrEmpty(cropped.ExceptionString))
                return BadRequest(cropped.ExceptionString);
            return Ok(cropped.Result);
        }


        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="pictureFileService">
        /// </param>
        public ImageController(IPictureFileService pictureFileService)
        {
            _pictureFileService = pictureFileService;
        }

        /// <summary>
        /// Artifact Service
        /// </summary>
        protected readonly IPictureFileService _pictureFileService;


    }
}
