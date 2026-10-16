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
    /// management lists of literary concepts: paging with usage counts and probable duplicates
    /// </summary>
    [Produces("application/json")]
    [Route("api/ganjoor/literary/concepts-admin")]
    public class GanjoorLiteraryConceptAdminController : Controller
    {
        /// <summary>
        /// a page of approved concepts with usage counts
        /// </summary>
        /// <param name="query">name filter</param>
        /// <param name="sort">name, usage-desc or usage-asc</param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("page")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(LiteraryConceptListPage))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetConceptsPage(string query = "", string sort = "name", int skip = 0, int take = 50)
        {
            var res = await _ganjoorService.GetLiteraryConceptsPageAsync(query, sort, skip, take);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// groups of concepts which are probably duplicates
        /// </summary>
        /// <param name="maxGroups"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("duplicates")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(LiteraryConceptDuplicateGroup[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetDuplicateGroups(int maxGroups = 100)
        {
            var res = await _ganjoorService.GetLiteraryConceptDuplicateGroupsAsync(maxGroups);
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
        public GanjoorLiteraryConceptAdminController(IGanjoorService ganjoorService)
        {
            _ganjoorService = ganjoorService;
        }
    }
}
