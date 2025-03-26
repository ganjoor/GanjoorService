using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RMuseum.Models.Generic.ViewModels;
using RMuseum.Services;
using RSecurityBackend.Models.Generic;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace RMuseum.Controllers
{
    [Produces("application/json")]
    [Route("api/contributions")]
    public class ContributionStatsController : Controller
    {
        /// <summary>
        /// approved edits daily
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("poem/corrections/daily")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<GroupedByDateViewModel>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetApprovedEditsGroupedByDateAsync([FromQuery] PagingParameterModel paging, Guid? userId = null)
        {
            var pagedResult = await _service.GetApprovedEditsGroupedByDateAsync(paging, userId);
            if (!string.IsNullOrEmpty(pagedResult.ExceptionString))
                return BadRequest(pagedResult.ExceptionString);

            // Paging Header
            HttpContext.Response.Headers.Append("paging-headers", JsonConvert.SerializeObject(pagedResult.Result.PagingMeta));

            return Ok(pagedResult.Result.Tracks);
        }

        /// <summary>
        /// approved edits grouped by user
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="day"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("poem/corrections/by/user")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<GroupedByUserViewModel>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetApprovedEditsGroupedByUserAsync([FromQuery] PagingParameterModel paging,DateTime? day, Guid? userId)
        {
            var pagedResult = await _service.GetApprovedEditsGroupedByUserAsync(paging, day, userId);
            if (!string.IsNullOrEmpty(pagedResult.ExceptionString))
                return BadRequest(pagedResult.ExceptionString);

            // Paging Header
            HttpContext.Response.Headers.Append("paging-headers", JsonConvert.SerializeObject(pagedResult.Result.PagingMeta));

            return Ok(pagedResult.Result.Tracks);
        }

        /// <summary>
        /// service
        /// </summary>
        protected readonly IContributionStatsService _service;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="service"></param>
        public ContributionStatsController(IContributionStatsService service)
        {
            _service = service;
        }
    }
}
