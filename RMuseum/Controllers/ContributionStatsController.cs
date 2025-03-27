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
        /// daily stats
        /// </summary>
        /// <param name="dataType">
        /// poem/corrections
        /// section/corrections
        /// cat/corrections
        /// cat/corrections
        /// suggestedsongs
        /// quoteds
        /// </param>
        /// <param name="paging"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("{dataType}/daily")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<GroupedByDateViewModel>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetApprovedEditsGroupedByDateAsync(string dataType, [FromQuery] PagingParameterModel paging, Guid? userId = null)
        {
            var cacheKey = $"{dataType}/daily/{DateTime.Now.Date}/{paging.PageSize}/{paging.PageNumber}/{userId ?? Guid.Empty}";
            if (!_memoryCache.TryGetValue(cacheKey, out (PaginationMetadata PagingMeta, GroupedByDateViewModel[] Tracks)  pagedResult))
            {
                switch (dataType)
                {
                    case "poem/corrections":
                        {
                            var res = await _service.GetApprovedEditsGroupedByDateAsync(paging, userId);
                            if (!string.IsNullOrEmpty(res.ExceptionString))
                                return BadRequest(res.ExceptionString);
                            pagedResult = res.Result;
                        }
                        break;
                    case "section/corrections":
                        {
                            var res = await _service.GetApprovedSectionEditsGroupedByDateAsync(paging, userId);
                            if (!string.IsNullOrEmpty(res.ExceptionString))
                                return BadRequest(res.ExceptionString);
                            pagedResult = res.Result;
                        }
                        break;
                    case "cat/corrections":
                        {
                            var res = await _service.GetApprovedCatEditsGroupedByDateAsync(paging, userId);
                            if (!string.IsNullOrEmpty(res.ExceptionString))
                                return BadRequest(res.ExceptionString);
                            pagedResult = res.Result;
                        }
                        break;
                    case "suggestedsongs":
                        {
                            var res = await _service.GetApprovedRelatedSongsGroupedByDateAsync(paging, userId);
                            if (!string.IsNullOrEmpty(res.ExceptionString))
                                return BadRequest(res.ExceptionString);
                            pagedResult = res.Result;
                        }
                        break;
                    case "quoteds":
                        {
                            var res = await _service.GetApprovedQuotedPoemsGroupedByDateAsync(paging, userId);
                            if (!string.IsNullOrEmpty(res.ExceptionString))
                                return BadRequest(res.ExceptionString);
                            pagedResult = res.Result;
                        }
                        break;
                    default:
                        return BadRequest("invalid value for the paramater: dataType");
                }
            }
            // Paging Header
            HttpContext.Response.Headers.Append("paging-headers", JsonConvert.SerializeObject(pagedResult.PagingMeta));

            return Ok(pagedResult.Tracks);
        }

        /// <summary>
        /// approved edits grouped by user
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="paging"></param>
        /// <param name="day"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("{dataType}/by/user")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<GroupedByUserViewModel>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetApprovedEditsGroupedByUserAsync(string dataType, [FromQuery] PagingParameterModel paging, DateTime? day, Guid? userId)
        {
            var cacheKey = $"{dataType}/by/user/{(day ?? DateTime.Now).Date}/{paging.PageSize}/{paging.PageNumber}/{userId ?? Guid.Empty}";
            if (!_memoryCache.TryGetValue(cacheKey, out (PaginationMetadata PagingMeta, GroupedByUserViewModel[] Tracks) pagedResult))
            {
                switch(dataType)
                {
                    case "poem/corrections":
                        {
                            var res = await _service.GetApprovedEditsGroupedByUserAsync(paging, day, userId);
                            if (!string.IsNullOrEmpty(res.ExceptionString))
                                return BadRequest(res.ExceptionString);
                            pagedResult = res.Result;
                        }
                        break;
                    case "section/corrections":
                        {
                            var res = await _service.GetApprovedSectionEditsGroupedByUserAsync(paging, day, userId);
                            if (!string.IsNullOrEmpty(res.ExceptionString))
                                return BadRequest(res.ExceptionString);
                            pagedResult = res.Result;
                        }
                        break;
                    case "cat/corrections":
                        {
                            var res = await _service.GetApprovedCatEditsGroupedByUserAsync(paging, day, userId);
                            if (!string.IsNullOrEmpty(res.ExceptionString))
                                return BadRequest(res.ExceptionString);
                            pagedResult = res.Result;
                        }
                        break;
                    case "suggestedsongs":
                        {
                            var res = await _service.GetApprovedRelatedSongsGroupedByUserAsync(paging, day, userId);
                            if (!string.IsNullOrEmpty(res.ExceptionString))
                                return BadRequest(res.ExceptionString);
                            pagedResult = res.Result;
                        }
                        break;
                    case "quoteds":
                        {
                            var res = await _service.GetApprovedQuotedPoemsGroupedByUserAsync(paging, day, userId);
                            if (!string.IsNullOrEmpty(res.ExceptionString))
                                return BadRequest(res.ExceptionString);
                            pagedResult = res.Result;
                        }
                        break;
                    default:
                        return BadRequest("invalid value for the paramater: dataType");
                }
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
        /// summed up stats of approved cat corrections
        /// </summary>
        /// <returns></returns>
        [HttpGet("cat/corrections")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(SummedUpViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetApprrovedCatEditsSummedUpStatsAsync()
        {
            if (!_memoryCache.TryGetValue($"cat/corrections/{DateTime.Now.Date}", out SummedUpViewModel result))
            {
                var res = await _service.GetApprrovedCatEditsSummedUpStatsAsync();
                if (!string.IsNullOrEmpty(res.ExceptionString))
                    return BadRequest(res.ExceptionString);
                result = res.Result;
            }
            return Ok(result);
        }


        /// <summary>
        /// summed up stats of approved related songs
        /// </summary>
        /// <returns></returns>
        [HttpGet("suggestedsongs")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(SummedUpViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetApprovedRelatedSongsSummedUpStatsAsync()
        {
            if (!_memoryCache.TryGetValue($"suggestedsongs/{DateTime.Now.Date}", out SummedUpViewModel result))
            {
                var res = await _service.GetApprovedRelatedSongsSummedUpStatsAsync();
                if (!string.IsNullOrEmpty(res.ExceptionString))
                    return BadRequest(res.ExceptionString);
                result = res.Result;
            }
            return Ok(result);
        }

       

        

        /// <summary>
        /// summed up stats of quoted poems
        /// </summary>
        /// <returns></returns>
        [HttpGet("quoteds")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(SummedUpViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetApprovedQuotedPoemsSummedUpStatsAsync()
        {
            if (!_memoryCache.TryGetValue($"quoteds/{DateTime.Now.Date}", out SummedUpViewModel result))
            {
                var res = await _service.GetApprovedQuotedPoemsSummedUpStatsAsync();
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
