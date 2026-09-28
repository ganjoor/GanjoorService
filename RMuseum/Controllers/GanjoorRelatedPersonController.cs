using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RMuseum.Models.Ganjoor;
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
        /// get person by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
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
