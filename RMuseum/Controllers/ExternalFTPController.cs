using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RMuseum.Models.Auth.Memory;
using RMuseum.Services;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using RMuseum.Models.ExternalFTPUpload;
using RSecurityBackend.Models.Generic;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;

namespace RMuseum.Controllers
{
    [Produces("application/json")]
    [Route("api/ftp")]
    public class ExternalFTPController : Controller
    {
        /// <summary>
        /// queued ftp uploads
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize(Policy = RMuseumSecurableItem.QueuedFTPUploadShortName + ":" + RMuseumSecurableItem.ModerateOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<QueuedFTPUpload>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetQueuedFTPUploadsAsync([FromQuery] PagingParameterModel paging)
        {

            RServiceResult<(PaginationMetadata PagingMeta, QueuedFTPUpload[] Items)> itemsInfo = await _ftpService.GetQueuedFTPUploadsAsync(paging);
            if (!string.IsNullOrEmpty(itemsInfo.ExceptionString))
            {
                return BadRequest(itemsInfo.ExceptionString);
            }

            // Paging Header
            HttpContext.Response.Headers.Append("paging-headers", JsonConvert.SerializeObject(itemsInfo.Result.PagingMeta));

            return Ok(itemsInfo.Result.Items);
        }

        /// <summary>
        /// process ftp queue
        /// </summary>
        /// <returns></returns>
        [HttpPost("start")]
        [Authorize(Policy = RMuseumSecurableItem.QueuedFTPUploadShortName + ":" + RMuseumSecurableItem.ModerateOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> ProcessQueueAsync()
        {
            var res = await _ftpService.ProcessQueueAsync(null);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok();
        }

        /// <summary>
        /// reset ftp queue
        /// </summary>
        /// <returns></returns>
        [HttpPost("reset")]
        [Authorize(Policy = RMuseumSecurableItem.QueuedFTPUploadShortName + ":" + RMuseumSecurableItem.ModerateOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> ResetQueueAsync()
        {
            var res = await _ftpService.ResetQueueAsync();
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok();
        }

        /// <summary>
        /// FTP Service
        /// </summary>
        protected readonly IQueuedFTPUploadService _ftpService;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="ftpService"></param>
        public ExternalFTPController(IQueuedFTPUploadService ftpService)
        {
            _ftpService = ftpService;
        }

    }
}
