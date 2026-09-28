using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Services;
using System.Net;
using System.Threading.Tasks;

namespace RMuseum.Controllers
{
    /// <summary>
    /// related people (family tree / person tagging) - read-only for now, see IGanjoorRelatedPersonService
    /// </summary>
    [Produces("application/json")]
    [Route("api/people")]
    public class GanjoorRelatedPersonController : Controller
    {
        /// <summary>
        /// get all people
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorRelatedPerson[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetPeopleAsync()
        {
            var res = await _personService.GetPeopleAsync();
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get people who caption a family tree - the entry points for browsing family trees.
        /// Registered before the "{id}" route below (and constrained to int there) so this literal
        /// segment isn't swallowed as an id.
        /// </summary>
        /// <returns></returns>
        [HttpGet("familytrees")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorRelatedPerson[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetFamilyTreeRootsAsync()
        {
            var res = await _personService.GetFamilyTreeRootsAsync();
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get person by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorRelatedPerson))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetPersonAsync(int id)
        {
            var res = await _personService.GetPersonAsync(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get a person along with all their kinship/affiliation edges, for the read-only
        /// person/family-tree browsing page
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id:int}/relations")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPersonRelationsViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetPersonRelationsAsync(int id)
        {
            var res = await _personService.GetPersonRelationsAsync(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get the poems tagged with this person
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id:int}/poems")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(PoemGeoDateTag[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetPoemsByPersonAsync(int id)
        {
            var res = await _personService.GetPoemsByPersonAsync(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// person service
        /// </summary>
        private readonly IGanjoorRelatedPersonService _personService;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="personService"></param>
        public GanjoorRelatedPersonController(IGanjoorRelatedPersonService personService)
        {
            _personService = personService;
        }
    }
}
