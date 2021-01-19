using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RMuseum.Models.MusicCatalogue.ViewModels;
using RMuseum.Services;
using RSecurityBackend.Models.Generic;
using System.Net;
using System.Threading.Tasks;

namespace RMuseum.Controllers
{
    [Produces("application/json")]
    [Route("api/musiccatalogue")]
    public class MusicCatalogueController : Controller
    {
        /// <summary>
        /// golha collection programs
        /// </summary>
        /// <param name="id">collection id</param>
        /// <returns></returns>
        [HttpGet]
        [Route("golha/collection/{id}/programs")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GolhaProgramViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetGolhaCollectionPrograms(int id)
        {
            RServiceResult<GolhaProgramViewModel[]> res =
                await _catalogueService.GetGolhaCollectionPrograms(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// program tracks
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("golha/program/{id}/tracks")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GolhaTrackViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetGolhaProgramTracks(int id)
        {
            RServiceResult<GolhaTrackViewModel[]> res =
                await _catalogueService.GetGolhaProgramTracks(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }


        /// <summary>
        /// Catalogue Service
        /// </summary>
        protected readonly IMusicCatalogueService _catalogueService;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="catalogueService"></param>
        public MusicCatalogueController(IMusicCatalogueService catalogueService)
        {
            _catalogueService = catalogueService;
        }
    }
}
