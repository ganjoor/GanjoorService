using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RMuseum.Models.Ganjoor;
using RMuseum.Services;
using System.Net;
using System.Threading.Tasks;

namespace RMuseum.Controllers
{
    /// <summary>
    /// public statistics and browsing of literary tags
    /// </summary>
    [Produces("application/json")]
    [Route("api/ganjoor/literary/stats")]
    public class GanjoorLiteraryStatsController : Controller
    {
        /// <summary>
        /// summary statistics (devices, most used concepts, poets)
        /// </summary>
        /// <param name="poetId">0 = all poets</param>
        /// <returns></returns>
        [HttpGet]
        [Route("overview")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(LiteraryStatsOverview))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetOverview(int poetId = 0)
        {
            var res = await _ganjoorService.GetLiteraryStatsOverviewAsync(poetId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// the most used concepts of a device
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="poetId"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("device/{deviceId}/top-concepts")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(LiteraryStatsConcept[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetDeviceTopConcepts(int deviceId, int poetId = 0, int take = 30)
        {
            var res = await _ganjoorService.GetLiteraryDeviceTopConceptsAsync(deviceId, poetId, take);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// tags of a device and/or a concept (with its descendants), a page at a time
        /// </summary>
        /// <param name="deviceId">0 = any</param>
        /// <param name="conceptId">0 = any</param>
        /// <param name="poetId">0 = all poets</param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("tags")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(LiteraryStatsTagPage))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetTags(int deviceId = 0, int conceptId = 0, int poetId = 0, int skip = 0, int take = 20)
        {
            var res = await _ganjoorService.GetLiteraryStatsTagsAsync(deviceId, conceptId, poetId, skip, take);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// Ganjoor Service
        /// </summary>
        protected readonly IGanjoorService _ganjoorService;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="ganjoorService"></param>
        public GanjoorLiteraryStatsController(IGanjoorService ganjoorService)
        {
            _ganjoorService = ganjoorService;
        }
    }
}
