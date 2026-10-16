using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RMuseum.Models.Auth.Memory;
using RMuseum.Models.Ganjoor;
using RMuseum.Services;
using RSecurityBackend.Models.Auth.Memory;
using System.Net;
using System.Threading.Tasks;

namespace RMuseum.Controllers
{
    /// <summary>
    /// sources of imported literary tags (academic corpora, books, ...)
    /// </summary>
    [Produces("application/json")]
    [Route("api/ganjoor/literary/sources")]
    public class GanjoorLiteraryTagSourceController : Controller
    {
        /// <summary>
        /// all sources
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(LiteraryTagSource[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetSources()
        {
            var res = await _ganjoorService.GetLiteraryTagSourcesAsync();
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// add (Id = 0) or update a source
        /// </summary>
        [HttpPut]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(LiteraryTagSource))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> SaveSource([FromBody] LiteraryTagSource source)
        {
            var res = await _ganjoorService.SaveLiteraryTagSourceAsync(source);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// delete a source
        /// </summary>
        [HttpDelete]
        [Route("{id}")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> DeleteSource(int id)
        {
            var res = await _ganjoorService.DeleteLiteraryTagSourceAsync(id);
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
        public GanjoorLiteraryTagSourceController(IGanjoorService ganjoorService)
        {
            _ganjoorService = ganjoorService;
        }
    }
}
