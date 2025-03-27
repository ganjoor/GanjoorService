using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using RMuseum.Models.Generic.ViewModels;
using RMuseum.Services;
using RSecurityBackend.Models.Generic;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

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
            var cacheKey = $"poem/corrections/daily/{DateTime.Now.Date}/{paging.PageSize}/{paging.PageNumber}/{userId ?? Guid.Empty}";
            if (!_memoryCache.TryGetValue(cacheKey, out (PaginationMetadata PagingMeta, GroupedByDateViewModel[] Tracks)  pagedResult))
            {
                var res = await _service.GetApprovedEditsGroupedByDateAsync(paging, userId);
                if (!string.IsNullOrEmpty(res.ExceptionString))
                    return BadRequest(res.ExceptionString);
                pagedResult = res.Result;
            }
            // Paging Header
            HttpContext.Response.Headers.Append("paging-headers", JsonConvert.SerializeObject(pagedResult.PagingMeta));

            return Ok(pagedResult.Tracks);
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
            var cacheKey = $"poem/corrections/by/user/{(day ?? DateTime.Now).Date}/{paging.PageSize}/{paging.PageNumber}/{userId ?? Guid.Empty}";
            if (!_memoryCache.TryGetValue(cacheKey, out (PaginationMetadata PagingMeta, GroupedByUserViewModel[] Tracks) pagedResult))
            {
                var res = await _service.GetApprovedEditsGroupedByUserAsync(paging, day, userId);
                if (!string.IsNullOrEmpty(res.ExceptionString))
                    return BadRequest(res.ExceptionString);
                pagedResult = res.Result;
            }

            // Paging Header
            HttpContext.Response.Headers.Append("paging-headers", JsonConvert.SerializeObject(pagedResult.PagingMeta));

            return Ok(pagedResult.Tracks);
        }

        /// <summary>
        /// summed up stats of approved poem corrections
        /// </summary>
        /// <returns></returns>
        [HttpGet("poem/corrections")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(SummedUpViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetApprrovedEditsSummedUpStatsAsync()
        {
            if (!_memoryCache.TryGetValue($"poem/corrections/{DateTime.Now.Date}", out SummedUpViewModel result))
            {
                var res = await _service.GetApprrovedEditsSummedUpStatsAsync();
                if (!string.IsNullOrEmpty(res.ExceptionString))
                    return BadRequest(res.ExceptionString);
                result = res.Result;
            }
            return Ok(result);
        }


        /// <summary>
        /// approved section edits daily
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("section/corrections/daily")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<GroupedByDateViewModel>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetApprovedSectionEditsGroupedByDateAsync([FromQuery] PagingParameterModel paging, Guid? userId = null)
        {
            var cacheKey = $"section/corrections/daily/{DateTime.Now.Date}/{paging.PageSize}/{paging.PageNumber}/{userId ?? Guid.Empty}";
            if (!_memoryCache.TryGetValue(cacheKey, out (PaginationMetadata PagingMeta, GroupedByDateViewModel[] Tracks) pagedResult))
            {
                var res = await _service.GetApprovedSectionEditsGroupedByDateAsync(paging, userId);
                if (!string.IsNullOrEmpty(res.ExceptionString))
                    return BadRequest(res.ExceptionString);
                pagedResult = res.Result;
            }
            // Paging Header
            HttpContext.Response.Headers.Append("paging-headers", JsonConvert.SerializeObject(pagedResult.PagingMeta));

            return Ok(pagedResult.Tracks);
        }

        /// <summary>
        /// approved section edits grouped by user
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="day"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("section/corrections/by/user")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<GroupedByUserViewModel>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetApprovedSectionEditsGroupedByUserAsync([FromQuery] PagingParameterModel paging, DateTime? day, Guid? userId)
        {
            var cacheKey = $"section/corrections/by/user/{(day ?? DateTime.Now).Date}/{paging.PageSize}/{paging.PageNumber}/{userId ?? Guid.Empty}";
            if (!_memoryCache.TryGetValue(cacheKey, out (PaginationMetadata PagingMeta, GroupedByUserViewModel[] Tracks) pagedResult))
            {
                var res = await _service.GetApprovedSectionEditsGroupedByUserAsync(paging, day, userId);
                if (!string.IsNullOrEmpty(res.ExceptionString))
                    return BadRequest(res.ExceptionString);
                pagedResult = res.Result;
            }

            // Paging Header
            HttpContext.Response.Headers.Append("paging-headers", JsonConvert.SerializeObject(pagedResult.PagingMeta));

            return Ok(pagedResult.Tracks);
        }

        /// <summary>
        /// summed up stats of approved section corrections
        /// </summary>
        /// <returns></returns>
        [HttpGet("section/corrections")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(SummedUpViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetApprrovedSedtionEditsSummedUpStatsAsync()
        {
            if (!_memoryCache.TryGetValue($"section/corrections/{DateTime.Now.Date}", out SummedUpViewModel result))
            {
                var res = await _service.GetApprrovedSectionEditsSummedUpStatsAsync();
                if (!string.IsNullOrEmpty(res.ExceptionString))
                    return BadRequest(res.ExceptionString);
                result = res.Result;
            }
            return Ok(result);
        }


        /// <summary>
        /// service
        /// </summary>
        protected readonly IContributionStatsService _service;

        /// <summary>
        /// IMemoryCache
        /// </summary>
        protected readonly IMemoryCache _memoryCache;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="service"></param>
        /// <param name="memoryCache"></param>
        public ContributionStatsController(IContributionStatsService service, IMemoryCache memoryCache)
        {
            _service = service;
            _memoryCache = memoryCache;
        }
    }
}
