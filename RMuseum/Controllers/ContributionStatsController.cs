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
        /// comments
        /// recitations
        /// museumlinks
        /// </param>
        /// <param name="paging"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("{dataType}/daily")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<GroupedByDateViewModel>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetDataGroupedByDateAsync(string dataType, [FromQuery] PagingParameterModel paging, Guid? userId = null)
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
                    case "comments":
                        {
                            var res = await _service.GetApprovedCommentsGroupedByDateAsync(paging, userId);
                            if (!string.IsNullOrEmpty(res.ExceptionString))
                                return BadRequest(res.ExceptionString);
                            pagedResult = res.Result;
                        }
                        break;
                    case "recitations":
                        {
                            var res = await _service.GetApprovedRecitationsGroupedByDateAsync(paging, userId);
                            if (!string.IsNullOrEmpty(res.ExceptionString))
                                return BadRequest(res.ExceptionString);
                            pagedResult = res.Result;
                        }
                        break;
                    case "museumlinks":
                        {
                            var res = await _service.GetApprovedMuseumLinksGroupedByDateAsync(paging, userId);
                            if (!string.IsNullOrEmpty(res.ExceptionString))
                                return BadRequest(res.ExceptionString);
                            pagedResult = res.Result;
                        }
                        break;
                    default:
                        return BadRequest($"Invalid value for the paramater: dataType = {dataType}");
                }
            }
            // Paging Header
            HttpContext.Response.Headers.Append("paging-headers", JsonConvert.SerializeObject(pagedResult.PagingMeta));

            return Ok(pagedResult.Tracks);
        }

        /// <summary>
        /// datatype grouped by user
        /// </summary>
        /// <param name="dataType">
        /// poem/corrections
        /// section/corrections
        /// cat/corrections
        /// cat/corrections
        /// suggestedsongs
        /// quoteds
        /// comments
        /// recitations
        /// museumlinks
        /// </param>
        /// <param name="paging"></param>
        /// <param name="day"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("{dataType}/by/user")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<GroupedByUserViewModel>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetDataGroupedByUserAsync(string dataType, [FromQuery] PagingParameterModel paging, DateTime? day, Guid? userId)
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
                    case "comments":
                        {
                            var res = await _service.GetApprovedCommentsGroupedByUserAsync(paging, day, userId);
                            if (!string.IsNullOrEmpty(res.ExceptionString))
                                return BadRequest(res.ExceptionString);
                            pagedResult = res.Result;
                        }
                        break;
                    case "recitations":
                        {
                            var res = await _service.GetApprovedRecitationsGroupedByUserAsync(paging, day, userId);
                            if (!string.IsNullOrEmpty(res.ExceptionString))
                                return BadRequest(res.ExceptionString);
                            pagedResult = res.Result;
                        }
                        break;
                    case "museumlinks":
                        {
                            var res = await _service.GetApprovedMuseumLinksGroupedByUserAsync(paging, day, userId);
                            if (!string.IsNullOrEmpty(res.ExceptionString))
                                return BadRequest(res.ExceptionString);
                            pagedResult = res.Result;
                        }
                        break;
                    default:
                        return BadRequest($"Invalid value for the paramater: dataType = {dataType}");
                }
            }

            // Paging Header
            HttpContext.Response.Headers.Append("paging-headers", JsonConvert.SerializeObject(pagedResult.PagingMeta));

            return Ok(pagedResult.Tracks);
        }

        /// <summary>
        /// summed up stats of data
        /// </summary>
        /// <param name="dataType">
        /// poem/corrections
        /// section/corrections
        /// cat/corrections
        /// cat/corrections
        /// suggestedsongs
        /// quoteds
        /// comments
        /// recitations
        /// museumlinks
        /// </param>        
        /// <returns></returns>
        [HttpGet("{dataType}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(SummedUpViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetDataSummedUpStatsAsync(string dataType)
        {
            if (!_memoryCache.TryGetValue($"{dataType}/{DateTime.Now.Date}", out SummedUpViewModel result))
            {
                switch(dataType)
                {
                    case "poem/corrections":
                        {
                            var res = await _service.GetApprrovedEditsSummedUpStatsAsync();
                            if (!string.IsNullOrEmpty(res.ExceptionString))
                                return BadRequest(res.ExceptionString);
                            result = res.Result;
                        }
                        break;
                    case "section/corrections":
                        {
                            var res = await _service.GetApprrovedSectionEditsSummedUpStatsAsync();
                            if (!string.IsNullOrEmpty(res.ExceptionString))
                                return BadRequest(res.ExceptionString);
                            result = res.Result;
                        }
                        break;
                    case "cat/corrections":
                        {
                            var res = await _service.GetApprrovedCatEditsSummedUpStatsAsync();
                            if (!string.IsNullOrEmpty(res.ExceptionString))
                                return BadRequest(res.ExceptionString);
                            result = res.Result;
                        }
                        break;
                    case "suggestedsongs":
                        {
                            var res = await _service.GetApprovedRelatedSongsSummedUpStatsAsync();
                            if (!string.IsNullOrEmpty(res.ExceptionString))
                                return BadRequest(res.ExceptionString);
                            result = res.Result;
                        }
                        break;
                    case "quoteds":
                        {
                            var res = await _service.GetApprovedQuotedPoemsSummedUpStatsAsync();
                            if (!string.IsNullOrEmpty(res.ExceptionString))
                                return BadRequest(res.ExceptionString);
                            result = res.Result;
                        }
                        break;
                    case "comments":
                        {
                            var res = await _service.GetApprovedCommentsSummedUpStatsAsync();
                            if (!string.IsNullOrEmpty(res.ExceptionString))
                                return BadRequest(res.ExceptionString);
                            result = res.Result;
                        }
                        break;
                    case "recitations":
                        {
                            var res = await _service.GetApprovedRecitationsSummedUpStatsAsync();
                            if (!string.IsNullOrEmpty(res.ExceptionString))
                                return BadRequest(res.ExceptionString);
                            result = res.Result;
                        }
                        break;
                    case "museumlinks":
                        {
                            var res = await _service.GetApprovedMuseumLinksSummedUpStatsAsync();
                            if (!string.IsNullOrEmpty(res.ExceptionString))
                                return BadRequest(res.ExceptionString);
                            result = res.Result;
                        }
                        break;
                    default:
                        return BadRequest($"Invalid value for the paramater: dataType = {dataType}");
                }
               
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
