using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RMuseum.Models.Artifact;
using RMuseum.Models.Artifact.ViewModels;
using RMuseum.Models.Auth.Memory;
using RMuseum.Models.Bookmark;
using RMuseum.Models.Bookmark.ViewModels;
using RMuseum.Models.GanjoorIntegration;
using RMuseum.Models.GanjoorIntegration.ViewModels;
using RMuseum.Models.ImportJob;
using RMuseum.Models.Note;
using RMuseum.Models.Note.ViewModels;
using RMuseum.Services;
using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace RMuseum.Controllers
{
    [Produces("application/json")]
    [Route("api/artifacts")]
    public class ArtifactController : Controller
    {
        /// <summary>
        /// get all published artifacts (including CoverImage info but not items info)
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<RArtifactMasterRecord>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]

        public async Task<IActionResult> Get([FromQuery] PagingParameterModel paging)
        {
            var cacheKey = $"artifacts/all/{paging.PageSize}/{paging.PageNumber}";
            if (!_memoryCache.TryGetValue(cacheKey, out RServiceResult<(PaginationMetadata PagingMeta, RArtifactMasterRecord[] Items)> itemsInfo))
            {
                itemsInfo = await _artifactService.GetAll(paging, new PublishStatus[] { PublishStatus.Published });
                if (!string.IsNullOrEmpty(itemsInfo.ExceptionString))
                {
                    return BadRequest(itemsInfo.ExceptionString);
                }

                _memoryCache.Set(cacheKey, itemsInfo);
            }

            if (itemsInfo.Result.Items.Count() > 0)
            {
                DateTime lastModification = itemsInfo.Result.Items.Max(i => i.LastModified);
                Response.GetTypedHeaders().LastModified = lastModification;

                var requestHeaders = Request.GetTypedHeaders();
                if (requestHeaders.IfModifiedSince.HasValue &&
                    requestHeaders.IfModifiedSince.Value >= lastModification)
                {
                    return StatusCode(StatusCodes.Status304NotModified);
                }
            }

            // Paging Header
            HttpContext.Response.Headers.Append("paging-headers", JsonConvert.SerializeObject(itemsInfo.Result.PagingMeta));

            return Ok(itemsInfo.Result.Items);
        }

        /// <summary>
        /// get list of artifact statuses user can see
        /// </summary>
        /// <param name="loggedOnUserId"></param>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        private async Task<RServiceResult<PublishStatus[]>> _GetUserVisibleArtifactStatusSet(Guid loggedOnUserId, Guid sessionId)
        {
            RServiceResult<bool>
                canView =
                await _userPermissionChecker.Check
                    (
                        loggedOnUserId,
                        sessionId,
                        User.Claims.Any(c => c.Type == "Language") ? User.Claims.First(c => c.Type == "Language").Value : "fa-IR",
                        RMuseumSecurableItem.ArtifactEntityShortName,
                        RMuseumSecurableItem.ViewDraftOperationShortName
                        );
            if (!string.IsNullOrEmpty(canView.ExceptionString))
                return new RServiceResult<PublishStatus[]>(null, canView.ExceptionString);

            PublishStatus[] visibleItems =
                canView.Result
                ?
                new PublishStatus[]
                {
                    PublishStatus.Published,
                    PublishStatus.Restricted,
                    PublishStatus.Draft,
                    PublishStatus.Awaiting
                }
                :
                 new PublishStatus[]
                {
                    PublishStatus.Published
                };

            return new RServiceResult<PublishStatus[]>(visibleItems);
        }

        /// <summary>
        /// get all artifacts visible by user (including CoverImage info but not items info)
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>

        [HttpGet]
        [Route("secure")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<RArtifactMasterRecord>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetUserVisible([FromQuery] PagingParameterModel paging)
        {
            RServiceResult<PublishStatus[]> v = await _GetUserVisibleArtifactStatusSet
                (
                new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value),
                new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value)
                );
            if (!string.IsNullOrEmpty(v.ExceptionString))
                return BadRequest(v.ExceptionString);
            PublishStatus[] visibleItems = v.Result;

            if (visibleItems.Length == 1 && visibleItems[0] == PublishStatus.Published) //Caching
            {
                return await Get(paging);
            }

            RServiceResult<(PaginationMetadata PagingMeta, RArtifactMasterRecord[] Items)> itemsInfo = await _artifactService.GetAll(paging, visibleItems);
            if (!string.IsNullOrEmpty(itemsInfo.ExceptionString))
            {
                return BadRequest(itemsInfo.ExceptionString);
            }

            if (itemsInfo.Result.Items.Count() > 0)
            {
                DateTime lastModification = itemsInfo.Result.Items.Max(i => i.LastModified);
                Response.GetTypedHeaders().LastModified = lastModification;

                var requestHeaders = Request.GetTypedHeaders();
                if (requestHeaders.IfModifiedSince.HasValue &&
                    requestHeaders.IfModifiedSince.Value >= lastModification)
                {
                    return StatusCode(StatusCodes.Status304NotModified);
                }
            }

            // Paging Header
            HttpContext.Response.Headers.Append("paging-headers", JsonConvert.SerializeObject(itemsInfo.Result.PagingMeta));

            return Ok(itemsInfo.Result.Items);
        }

        [HttpGet("tagged/{tagUrl}/{valueUrl}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<RArtifactMasterRecord>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetByTagValue(string tagUrl, string valueUrl)
        {
            RServiceResult<RArtifactMasterRecord[]> itemsInfo = await _artifactService.GetByTagValue(tagUrl, valueUrl, new PublishStatus[] { PublishStatus.Published });
            if (!string.IsNullOrEmpty(itemsInfo.ExceptionString))
            {
                return BadRequest(itemsInfo.ExceptionString);
            }

            if (itemsInfo.Result.Length > 0)
            {
                DateTime lastModification = itemsInfo.Result.Max(i => i.LastModified);
                Response.GetTypedHeaders().LastModified = lastModification;

                var requestHeaders = Request.GetTypedHeaders();
                if (requestHeaders.IfModifiedSince.HasValue &&
                    requestHeaders.IfModifiedSince.Value >= lastModification)
                {
                    return StatusCode(StatusCodes.Status304NotModified);
                }
            }

            return Ok(itemsInfo.Result);
        }


        /// <summary>
        /// gets specified publish artifact info (including CoverImage + images)
        /// </summary>
        /// <param name="friendlyUrl"></param>
        /// <returns></returns>
        [HttpGet("{friendlyUrl}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RArtifactMasterRecordViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Get(string friendlyUrl)
        {
            var cacheKey = $"artifacts/friendlyUrl/{friendlyUrl}";
            if (!_memoryCache.TryGetValue(cacheKey, out RServiceResult<RArtifactMasterRecordViewModel> itemInfo))
            {
                itemInfo = await _artifactService.GetByFriendlyUrl(friendlyUrl, new PublishStatus[] { PublishStatus.Published });
                if (!string.IsNullOrEmpty(itemInfo.ExceptionString))
                {
                    return BadRequest(itemInfo.ExceptionString);
                }
                if (itemInfo.Result == null)
                    return NotFound();

                _memoryCache.Set(cacheKey, itemInfo);
            }


            Response.GetTypedHeaders().LastModified = itemInfo.Result.LastModified;

            var requestHeaders = Request.GetTypedHeaders();
            if (requestHeaders.IfModifiedSince.HasValue &&
                requestHeaders.IfModifiedSince.Value >= itemInfo.Result.LastModified)
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            return Ok(itemInfo.Result);
        }

        /// <summary>
        /// remove unpublished artifact having no notes and not bookmarked
        /// </summary>
        /// <param name="artifactId"></param>
        /// <returns></returns>
        [HttpDelete("{artifactId}")]
        [Authorize(Policy = RMuseumSecurableItem.ArtifactEntityShortName + ":" + SecurableItem.DeleteOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> RemoveArtifact(Guid artifactId)
        {
            RServiceResult<bool> res = await _artifactService.RemoveArtifact(artifactId, true);
            if (!string.IsNullOrEmpty(res.ExceptionString))
            {
                return BadRequest(res.ExceptionString);
            }
            return Ok(res.Result);
        }

        /// <summary>
        /// gets specified publish artifact info (including CoverImage + images) where its items are filteted by tag
        /// </summary>
        /// <param name="friendlyUrl"></param>
        /// <param name="tagFriendlyUrl"></param>
        /// <returns></returns>
        [HttpGet("{friendlyUrl}/filteritemsbytag/{tagFriendlyUrl}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RArtifactMasterRecordViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetByFriendlyUrlFilterItemsByTag(string friendlyUrl, string tagFriendlyUrl)
        {
            return await GetByFriendlyUrlFilterItemsByTag(friendlyUrl, tagFriendlyUrl, null);
        }

        /// <summary>
        /// gets specified publish artifact info (including CoverImage + images) where its items are filteted by tag
        /// </summary>
        /// <param name="friendlyUrl"></param>
        /// <param name="tagFriendlyUrl"></param>
        /// <param name="valueFriendlyUrl"></param>
        /// <returns></returns>
        [HttpGet("{friendlyUrl}/filteritemsbytag/{tagFriendlyUrl}/{valueFriendlyUrl}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RArtifactMasterRecordViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetByFriendlyUrlFilterItemsByTag(string friendlyUrl, string tagFriendlyUrl, string valueFriendlyUrl)
        {
            var cacheKey = $"{friendlyUrl}/filteritemsbytag/{tagFriendlyUrl}";
            if (valueFriendlyUrl != null)
            {
                cacheKey += $"/{valueFriendlyUrl}";
            }
            if (!_memoryCache.TryGetValue(cacheKey, out RServiceResult<RArtifactMasterRecordViewModel> itemInfo))
            {
                itemInfo = await _artifactService.GetByFriendlyUrlFilterItemsByTag(friendlyUrl, new PublishStatus[] { PublishStatus.Published }, tagFriendlyUrl, valueFriendlyUrl);
                if (!string.IsNullOrEmpty(itemInfo.ExceptionString))
                {
                    return BadRequest(itemInfo.ExceptionString);
                }
                if (itemInfo.Result == null)
                    return NotFound();

                _memoryCache.Set(cacheKey, itemInfo);
            }

            Response.GetTypedHeaders().LastModified = itemInfo.Result.LastModified;

            var requestHeaders = Request.GetTypedHeaders();
            if (requestHeaders.IfModifiedSince.HasValue &&
                requestHeaders.IfModifiedSince.Value >= itemInfo.Result.LastModified)
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            return Ok(itemInfo.Result);
        }

        /// <summary>
        /// gets specified publish artifact info (including CoverImage + images)
        /// </summary>
        /// <param name="friendlyUrl"></param>
        /// <returns></returns>
        [HttpGet("secure/{friendlyUrl}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RArtifactMasterRecordViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetUserVisible(string friendlyUrl)
        {
            RServiceResult<PublishStatus[]> v = await _GetUserVisibleArtifactStatusSet
               (
               new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value),
               new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value)
               );
            if (!string.IsNullOrEmpty(v.ExceptionString))
                return BadRequest(v.ExceptionString);
            PublishStatus[] visibleItems = v.Result;
            RServiceResult<RArtifactMasterRecordViewModel> itemInfo = null;
            if (visibleItems.Length == 1 && visibleItems[0] == PublishStatus.Published)
            {
                var cacheKey = $"artifacts/friendlyUrl/{friendlyUrl}";
                if (!_memoryCache.TryGetValue(cacheKey, out itemInfo))
                {
                    itemInfo = await _artifactService.GetByFriendlyUrl(friendlyUrl, new PublishStatus[] { PublishStatus.Published });
                    if (!string.IsNullOrEmpty(itemInfo.ExceptionString))
                    {
                        return BadRequest(itemInfo.ExceptionString);
                    }
                    if (itemInfo.Result == null)
                        return NotFound();

                    _memoryCache.Set(cacheKey, itemInfo);
                }
            }
            if (itemInfo == null)
            {
                itemInfo = await _artifactService.GetByFriendlyUrl(friendlyUrl, visibleItems);
            }

            if (!string.IsNullOrEmpty(itemInfo.ExceptionString))
            {
                return BadRequest(itemInfo.ExceptionString);
            }
            if (itemInfo.Result == null)
                return NotFound();

            Response.GetTypedHeaders().LastModified = itemInfo.Result.LastModified;

            var requestHeaders = Request.GetTypedHeaders();
            if (requestHeaders.IfModifiedSince.HasValue &&
                requestHeaders.IfModifiedSince.Value >= itemInfo.Result.LastModified)
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }


            return Ok(itemInfo.Result);
        }

        /// <summary>
        /// gets specified publish artifact info (including CoverImage + images) where its items are filteted by tag
        /// </summary>
        /// <param name="friendlyUrl"></param>
        /// <param name="tagFriendlyUrl"></param>
        /// <returns></returns>
        [HttpGet("secure/{friendlyUrl}/filteritemsbytag/{tagFriendlyUrl}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RArtifactMasterRecordViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetUserVisibleByFriendlyUrlFilterItemsByTag(string friendlyUrl, string tagFriendlyUrl)
        {
            return await GetUserVisibleByFriendlyUrlFilterItemsByTag(friendlyUrl, tagFriendlyUrl, null);
        }

        /// <summary>
        /// gets specified publish artifact info (including CoverImage + images) where its items are filteted by tag
        /// </summary>
        /// <param name="friendlyUrl"></param>
        /// <param name="tagFriendlyUrl"></param>
        /// <param name="valueFriendlyUrl"></param>
        /// <returns></returns>
        [HttpGet("secure/{friendlyUrl}/filteritemsbytag/{tagFriendlyUrl}/{valueFriendlyUrl}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RArtifactMasterRecordViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetUserVisibleByFriendlyUrlFilterItemsByTag(string friendlyUrl, string tagFriendlyUrl, string valueFriendlyUrl)
        {
            RServiceResult<PublishStatus[]> v = await _GetUserVisibleArtifactStatusSet
               (
               new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value),
               new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value)
               );
            if (!string.IsNullOrEmpty(v.ExceptionString))
                return BadRequest(v.ExceptionString);
            PublishStatus[] visibleItems = v.Result;
            if (visibleItems.Length == 1 && visibleItems[0] == PublishStatus.Published)
            {
                return await GetByFriendlyUrlFilterItemsByTag(friendlyUrl, tagFriendlyUrl, valueFriendlyUrl);
            }
            RServiceResult<RArtifactMasterRecordViewModel> itemInfo = await _artifactService.GetByFriendlyUrlFilterItemsByTag(friendlyUrl, visibleItems, tagFriendlyUrl, valueFriendlyUrl);
            if (!string.IsNullOrEmpty(itemInfo.ExceptionString))
            {
                return BadRequest(itemInfo.ExceptionString);
            }
            if (itemInfo.Result == null)
                return NotFound();

            Response.GetTypedHeaders().LastModified = itemInfo.Result.LastModified;

            var requestHeaders = Request.GetTypedHeaders();
            if (requestHeaders.IfModifiedSince.HasValue &&
                requestHeaders.IfModifiedSince.Value >= itemInfo.Result.LastModified)
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            return Ok(itemInfo.Result);
        }

        /// <summary>
        /// edit artifactt master record (user should have additional permissions artifact:awaiting and artifact:publish to change status of artifact)
        /// </summary>
        /// <remarks>
        /// editing related collections such as images and attributed or complex properties such as CoverImage is ignored
        /// </remarks>
        /// <param name="id"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        [HttpPut("{id}")]
        [Authorize(Policy = RMuseumSecurableItem.ArtifactEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Put(Guid id, [FromBody] RArtifactMasterRecord item)
        {
            if (id != item.Id)
            {
                return BadRequest();
            }

            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            Guid sessionId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value);

            RServiceResult<bool>
                canChangeStatusToAwaiting =
                await _userPermissionChecker.Check
                    (
                        loggedOnUserId,
                        sessionId,
                        User.Claims.Any(c => c.Type == "Language") ? User.Claims.First(c => c.Type == "Language").Value : "fa-IR",
                        RMuseumSecurableItem.ArtifactEntityShortName,
                        RMuseumSecurableItem.ToAwaitingStatusOperationShortName
                        );
            if (!string.IsNullOrEmpty(canChangeStatusToAwaiting.ExceptionString))
                return BadRequest(canChangeStatusToAwaiting.ExceptionString);

            RServiceResult<bool>
                canPublish =
                await _userPermissionChecker.Check
                    (
                        loggedOnUserId,
                        sessionId,
                        User.Claims.Any(c => c.Type == "Language") ? User.Claims.First(c => c.Type == "Language").Value : "fa-IR",
                        RMuseumSecurableItem.ArtifactEntityShortName,
                        RMuseumSecurableItem.PublishOperationShortName
                        );
            if (!string.IsNullOrEmpty(canPublish.ExceptionString))
                return BadRequest(canPublish.ExceptionString);

            RServiceResult<RArtifactMasterRecord> itemInfo = await _artifactService.EditMasterRecord(item, canChangeStatusToAwaiting.Result, canPublish.Result);
            if (!string.IsNullOrEmpty(itemInfo.ExceptionString))
            {
                return BadRequest(itemInfo.ExceptionString);
            }

            if (itemInfo == null)
            {
                return NotFound();
            }

            return Ok(); ;
        }

        /// <summary>
        /// Set Artifact Cover Item Index
        /// </summary>
        /// <param name="id"></param>
        /// <param name="itemIndex"></param>
        /// <returns></returns>
        [HttpPut("{id}/cover")]
        [Authorize(Policy = RMuseumSecurableItem.ArtifactEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> SetArtifactCoverItemIndex(Guid id, [FromBody] int itemIndex)
        {
            RServiceResult<bool> res = await _artifactService.SetArtifactCoverItemIndex(id, itemIndex);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest();
            return Ok();
        }

        /// <summary>
        /// get tag bundle by frindly url
        /// </summary>
        /// <param name="friendlyUrl"></param>
        /// <returns></returns>
        [HttpGet("tagbundle/{friendlyUrl}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RTagBundleViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetTagBundleByFiendlyUrl(string friendlyUrl)
        {
            var cacheKey = $"tagbundle/{friendlyUrl}";
            if (!_memoryCache.TryGetValue(cacheKey, out RServiceResult<RTagBundleViewModel> tag))
            {
                tag = await _artifactService.GetTagBundleByFiendlyUrl(friendlyUrl);

                if (!string.IsNullOrEmpty(tag.ExceptionString))
                {
                    return BadRequest(tag.ExceptionString);
                }
                if (tag.Result == null)
                    return NotFound();

                _memoryCache.Set(cacheKey, tag);
            }



            RServiceResult<DateTime> lastModified = await _artifactService.GetMaxArtifactLastModified();
            if (!string.IsNullOrEmpty(lastModified.ExceptionString))
            {
                return BadRequest(lastModified.ExceptionString);
            }

            DateTime lastModification = lastModified.Result;
            Response.GetTypedHeaders().LastModified = lastModification;

            var requestHeaders = Request.GetTypedHeaders();
            if (requestHeaders.IfModifiedSince.HasValue &&
                requestHeaders.IfModifiedSince.Value >= lastModification)
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            return Ok(tag.Result);
        }

        /// <summary>
        /// add new tag
        /// </summary>
        /// <param name="tag">only name is processed</param>
        /// <returns></returns>
        [HttpPost("tag")]
        [Authorize(Policy = RMuseumSecurableItem.TagEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RTag))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> NewTag([FromBody] RTag tag)
        {
            RServiceResult<RTag> res = await _artifactService.AddTag(tag.Name);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get all tags
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>
        [HttpGet("tag")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<RTag>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetTags([FromQuery] PagingParameterModel paging)
        {
            RServiceResult<(PaginationMetadata PagingMeta, RTag[] Items)> itemsInfo = await _artifactService.GetAllTags(paging);
            if (!string.IsNullOrEmpty(itemsInfo.ExceptionString))
            {
                return BadRequest(itemsInfo.ExceptionString);
            }
            // Paging Header
            HttpContext.Response.Headers.Append("paging-headers", JsonConvert.SerializeObject(itemsInfo.Result.PagingMeta));

            return Ok(itemsInfo.Result.Items);
        }

        [HttpGet("tag/{friendlyUrl}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RTag))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetTagByFriendlyUrl(string friendlyUrl)
        {
            RServiceResult<RTag> tag = await _artifactService.GetTagByFriendlyUrl(friendlyUrl);
            if (!string.IsNullOrEmpty(tag.ExceptionString))
            {
                return BadRequest(tag.ExceptionString);
            }

            return Ok(tag.Result);
        }


        /// <summary>
        /// edit tag
        /// </summary>
        /// <remarks>
        /// editable fields are limited
        /// </remarks>
        /// <param name="tagid"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        [HttpPut("tag/{tagid}")]
        [Authorize(Policy = RMuseumSecurableItem.TagEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> PutTag(Guid tagid, [FromBody] RTag tag)
        {
            if (tagid != tag.Id)
            {
                return BadRequest();
            }

            RServiceResult<RTag> itemInfo = await _artifactService.EditTag(tag);
            if (!string.IsNullOrEmpty(itemInfo.ExceptionString))
            {
                return BadRequest(itemInfo.ExceptionString);
            }

            if (itemInfo == null)
            {
                return NotFound();
            }

            return Ok(); ;
        }

        /// <summary>
        /// changes order of tags based on their position in artifacts
        /// </summary>
        /// <param name="tagId"></param>
        /// <param name="artifactId"></param>
        /// <param name="upordown">up / down</param>
        /// <returns>the other tag which its Order got replaced with the input id</returns>
        [HttpPut("tag/move/{tagid}/in/{artifactId}/{upordown}")]
        [Authorize(Policy = RMuseumSecurableItem.TagEntityShortName + ":" + RMuseumSecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(Guid))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> MoveTag(Guid tagId, Guid artifactId, string upordown)
        {


            RServiceResult<Guid?> otherTagId = await _artifactService.EditTagOrderBasedOnArtifact(tagId, artifactId, upordown == "up");
            if (!string.IsNullOrEmpty(otherTagId.ExceptionString))
            {
                return BadRequest(otherTagId.ExceptionString);
            }

            return Ok((Guid)otherTagId.Result);
        }

        /// <summary>
        /// changes order of tags based on their position in artifact items
        /// </summary>
        /// <param name="tagId"></param>
        /// <param name="itemId"></param>
        /// <param name="upordown">up / down</param>
        /// <returns>the other tag which its Order got replaced with the input id</returns>
        [HttpPut("tag/move/{tagid}/in/item/{itemId}/{upordown}")]
        [Authorize(Policy = RMuseumSecurableItem.TagEntityShortName + ":" + RMuseumSecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(Guid))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> MoveTagInItem(Guid tagId, Guid itemId, string upordown)
        {


            RServiceResult<Guid?> otherTagId = await _artifactService.EditTagOrderBasedOnItem(tagId, itemId, upordown == "up");
            if (!string.IsNullOrEmpty(otherTagId.ExceptionString))
            {
                return BadRequest(otherTagId.ExceptionString);
            }

            return Ok((Guid)otherTagId.Result);
        }

        /// <summary>
        /// get tag value bundle by frindly url
        /// </summary>
        /// <param name="friendlyUrl"></param>
        /// <param name="valueUrl"></param>
        /// <returns></returns>
        [HttpGet("tagbundle/{friendlyUrl}/{valueUrl}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RArtifactTagViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetTagValueBundleByFiendlyUrl(string friendlyUrl, string valueUrl)
        {
            RServiceResult<RArtifactTagViewModel> tag = await _artifactService.GetTagValueBundleByFiendlyUrl(friendlyUrl, valueUrl);
            if (!string.IsNullOrEmpty(tag.ExceptionString))
            {
                return BadRequest(tag.ExceptionString);
            }
            if (tag.Result == null)
                return NotFound();
            return Ok(tag.Result);
        }

        /// <summary>
        /// get tag value by frindly url
        /// </summary>
        /// <param name="tagId"></param>
        /// <param name="friendlyUrl"></param>
        /// <returns></returns>
        [HttpGet("tagvalue/{tagId}/{friendlyUrl}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RTagValue))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetTagValueByFriendlyUrl(Guid tagId, string friendlyUrl)
        {
            RServiceResult<RTagValue> tag = await _artifactService.GetTagValueByFriendlyUrl(tagId, friendlyUrl);
            if (!string.IsNullOrEmpty(tag.ExceptionString))
            {
                return BadRequest(tag.ExceptionString);
            }

            return Ok(tag.Result);
        }

        /// <summary>
        /// add new tag value to artifact
        /// </summary>
        /// <param name="artifactId"></param>
        /// <param name="tag">only name is processed</param>
        /// <returns></returns>
        [HttpPost("tagvalue/{artifactid}")]
        [Authorize(Policy = RMuseumSecurableItem.ArtifactEntityShortName + ":" + RMuseumSecurableItem.EditTagValueOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RTagValue))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> NewArtifactTagValue(Guid artifactId, [FromBody] RTag tag)
        {
            RServiceResult<RTagValue> res = await _artifactService.TagArtifact(artifactId, tag);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }


        /// <summary>
        /// edit artifact attribute value
        /// </summary>
        /// <remarks>
        /// editable fields are limited
        /// </remarks>
        /// <param name="artifactid"></param>
        /// <param name="tagvalue"></param>
        /// <param name="global">apply on all same value tags</param>
        /// <returns></returns>
        [HttpPut("tagvalue/{artifactid}/{global=true}")]
        [Authorize(Policy = RMuseumSecurableItem.ArtifactEntityShortName + ":" + RMuseumSecurableItem.EditTagValueOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> PutAttributeValue(Guid artifactid, bool global, [FromBody] RTagValue tagvalue)
        {

            RServiceResult<RTagValue> itemInfo = await _artifactService.EditTagValue(artifactid, tagvalue, global);
            if (!string.IsNullOrEmpty(itemInfo.ExceptionString))
            {
                return BadRequest(itemInfo.ExceptionString);
            }

            if (itemInfo == null)
            {
                return NotFound();
            }

            return Ok(); ;
        }

        /// <summary>
        /// remove tag from artifact
        /// </summary>
        /// <param name="artifactId"></param>
        /// <param name="tagValueId"></param>
        /// <returns></returns>
        [HttpDelete("tagvalue/{artifactId}/{tagValueId}")]
        [Authorize(Policy = RMuseumSecurableItem.ArtifactEntityShortName + ":" + RMuseumSecurableItem.EditTagValueOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> RemoveArtifactTagValue(Guid artifactId, Guid tagValueId)
        {
            RServiceResult<bool> res = await _artifactService.UnTagArtifact(artifactId, tagValueId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
            {
                return BadRequest(res.ExceptionString);
            }
            return Ok(res.Result);
        }

        /// <summary>
        /// add new tag value to item
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="tag">only name is processed</param>
        /// <returns></returns>
        [HttpPost("itemtagvalue/{itemId}")]
        [Authorize(Policy = RMuseumSecurableItem.ArtifactEntityShortName + ":" + RMuseumSecurableItem.EditTagValueOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RTagValue))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> NewItemTagValue(Guid itemId, [FromBody] RTag tag)
        {
            RServiceResult<RTagValue> res = await _artifactService.TagItem(itemId, tag);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// edit item attribute value
        /// </summary>
        /// <remarks>
        /// editable fields are limited
        /// </remarks>
        /// <param name="itemId"></param>
        /// <param name="tagvalue"></param>
        /// <param name="global">apply on all same value tags</param>
        /// <returns></returns>
        [HttpPut("itemtagvalue/{itemId}/{global=true}")]
        [Authorize(Policy = RMuseumSecurableItem.ArtifactEntityShortName + ":" + RMuseumSecurableItem.EditTagValueOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> PutItemTagValue(Guid itemId, bool global, [FromBody] RTagValue tagvalue)
        {

            RServiceResult<RTagValue> itemInfo = await _artifactService.EditItemTagValue(itemId, tagvalue, global);
            if (!string.IsNullOrEmpty(itemInfo.ExceptionString))
            {
                return BadRequest(itemInfo.ExceptionString);
            }

            if (itemInfo == null)
            {
                return NotFound();
            }

            return Ok(); ;
        }

        /// <summary>
        /// remove tag from item
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="tagValueId"></param>
        /// <returns></returns>
        [HttpDelete("itemtagvalue/{itemId}/{tagValueId}")]
        [Authorize(Policy = RMuseumSecurableItem.ArtifactEntityShortName + ":" + RMuseumSecurableItem.EditTagValueOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> RemoveItemTagValue(Guid itemId, Guid tagValueId)
        {
            RServiceResult<bool> res = await _artifactService.UnTagItem(itemId, tagValueId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
            {
                return BadRequest(res.ExceptionString);
            }
            return Ok(res.Result);
        }

        /// <summary>
        /// changes order of tag values based on their position in an artifact
        /// </summary>
        /// <param name="artifactId"></param>
        /// <param name="tagId"></param>
        /// <param name="valueId"></param>
        /// <param name="upordown">up / down</param>
        /// <returns>the other tag which its Order got replaced with the input id</returns>
        [HttpPut("tagvalue/{artifactid}/{tagId}/{valueId}/move/{upordown}")]
        [Authorize(Policy = RMuseumSecurableItem.ArtifactEntityShortName + ":" + RMuseumSecurableItem.EditTagValueOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(Guid))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> MoveTagValue(Guid artifactId, Guid tagId, Guid valueId, string upordown)
        {


            RServiceResult<Guid?> otherValueId = await _artifactService.EditTagValueOrder(artifactId, tagId, valueId, upordown == "up");
            if (!string.IsNullOrEmpty(otherValueId.ExceptionString))
            {
                return BadRequest(otherValueId.ExceptionString);
            }

            return Ok((Guid)otherValueId.Result);
        }

        /// <summary>
        /// changes order of tag values based on their position in an item
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="tagId"></param>
        /// <param name="valueId"></param>
        /// <param name="upordown">up / down</param>
        /// <returns>the other tag which its Order got replaced with the input id</returns>
        [HttpPut("itemtagvalue/{itemId}/{tagId}/{valueId}/move/{upordown}")]
        [Authorize(Policy = RMuseumSecurableItem.ArtifactEntityShortName + ":" + RMuseumSecurableItem.EditTagValueOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(Guid))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> MoveItemTagValue(Guid itemId, Guid tagId, Guid valueId, string upordown)
        {


            RServiceResult<Guid?> otherValueId = await _artifactService.EditItemTagValueOrder(itemId, tagId, valueId, upordown == "up");
            if (!string.IsNullOrEmpty(otherValueId.ExceptionString))
            {
                return BadRequest(otherValueId.ExceptionString);
            }

            return Ok((Guid)otherValueId.Result);
        }



        /// <summary>
        /// gets specified publish artifact item info (including images + attributes)
        /// </summary>
        /// <param name="artifactUrl"></param>
        /// <param name="itemUrl"></param>
        /// <returns></returns>
        [HttpGet("{artifactUrl}/{itemUrl}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RArtifactItemRecordViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetArtifactItemByFrienlyUrl(string artifactUrl, string itemUrl)
        {
            RServiceResult<RArtifactItemRecordViewModel> itemInfo = await _artifactService.GetArtifactItemByFrienlyUrl(artifactUrl, itemUrl, new PublishStatus[] { PublishStatus.Published });
            if (!string.IsNullOrEmpty(itemInfo.ExceptionString))
            {
                return BadRequest(itemInfo.ExceptionString);
            }
            if (itemInfo.Result == null)
                return NotFound();

            Response.GetTypedHeaders().LastModified = itemInfo.Result.Item.LastModified;

            var requestHeaders = Request.GetTypedHeaders();
            if (requestHeaders.IfModifiedSince.HasValue &&
                requestHeaders.IfModifiedSince.Value >= itemInfo.Result.Item.LastModified)
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            return Ok(itemInfo.Result);
        }
        /// <summary>
        /// gets specified publish artifact item info (including images + attributes) 
        /// </summary>
        /// <param name="artifactUrl"></param>
        /// <param name="itemUrl"></param>
        /// <returns></returns>
        [HttpGet("secure/{artifactUrl}/{itemUrl}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RArtifactItemRecordViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetArtifactItemByFrienlyUrlUserVisible(string artifactUrl, string itemUrl)
        {
            RServiceResult<PublishStatus[]> v = await _GetUserVisibleArtifactStatusSet
               (
               new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value),
               new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value)
               );
            if (!string.IsNullOrEmpty(v.ExceptionString))
                return BadRequest(v.ExceptionString);
            PublishStatus[] visibleItems = v.Result;

            RServiceResult<RArtifactItemRecordViewModel> itemInfo = await _artifactService.GetArtifactItemByFrienlyUrl(artifactUrl, itemUrl, visibleItems);
            if (!string.IsNullOrEmpty(itemInfo.ExceptionString))
            {
                return BadRequest(itemInfo.ExceptionString);
            }
            if (itemInfo.Result == null)
                return NotFound();

            Response.GetTypedHeaders().LastModified = itemInfo.Result.Item.LastModified;

            var requestHeaders = Request.GetTypedHeaders();
            if (requestHeaders.IfModifiedSince.HasValue &&
                requestHeaders.IfModifiedSince.Value >= itemInfo.Result.Item.LastModified)
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            return Ok(itemInfo.Result);
        }



        /// <summary>
        /// Add new artifact (multipart/form-data)
        /// 
        /// 
        ///     const data = new FormData();
        ///     data.append('name', 'تست');
        ///     data.append('description', '');
        ///     data.append('srcUrl', '');
        ///     data.append('picTitle', 'تست');
        ///     data.append('picDescription', '');
        ///     data.append('file', this.file);
        ///     data.append('picSrcUrl', '');
        ///     
        /// 
        /// </summary>        
        /// <returns></returns>

        [HttpPost]
        [Authorize(Policy = RMuseumSecurableItem.ArtifactEntityShortName + ":" + SecurableItem.AddOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RArtifactMasterRecord))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public virtual async Task<IActionResult> Post()
        {

            try
            {
                NewArtifact newArtifactInfo =
                new NewArtifact()
                {
                    Name = Request.Form["name"],
                    Description = Request.Form["description"],
                    SrcUrl = Request.Form["srcUrl"],
                    PicTitle = Request.Form["picTitle"],
                    PicDescription = Request.Form["picDescription"],
                    File = Request.Form.Files[0],
                    PicSrcUrl = Request.Form["picSrcUrl"]
                };

                RServiceResult<RArtifactMasterRecord> result =
                    await _artifactService.Add
                    (
                        newArtifactInfo.Name, newArtifactInfo.Description, newArtifactInfo.SrcUrl,
                        newArtifactInfo.PicTitle, newArtifactInfo.PicDescription, newArtifactInfo.File,
                        newArtifactInfo.PicSrcUrl,
                        null);

                if (result.Result == null)
                    return BadRequest(result.ExceptionString);
                return Ok(result.Result);
            }
            catch (Exception exp)
            {
                return BadRequest(exp.ToString());
            }

        }

        /// <summary>
        /// import from external resources
        /// </summary>
        /// <param name="srcType">pdf/loc/princeton/harvard/qajarwomen/hathitrust/penn/cam/bl/folder/walters/cbl/append</param>
        /// <param name="resourceNumber">119/foldername</param>
        /// <param name="friendlyUrl">golestan-baysonghori/artifact id</param>
        /// <param name="resourcePrefix"></param>
        /// <param name="skipUpload"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("import")]
        [Authorize(Policy = RMuseumSecurableItem.ArtifactEntityShortName + ":" + RMuseumSecurableItem.ImportOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> Import(string srcType, string resourceNumber, string friendlyUrl, string resourcePrefix, bool skipUpload)
        {
            RServiceResult<bool> res =
                await _artifactService.Import(srcType, resourceNumber, friendlyUrl, resourcePrefix, skipUpload);
            if (res.Result)
                return Ok();
            return BadRequest(res.ExceptionString);
        }

        /// <summary>
        /// keep alive (for import background service)
        /// </summary>
        /// <returns>true</returns>
        [HttpGet]
        [Route("keep-alive")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public IActionResult KeepAlive()
        {
            return Ok(true);
        }


        /// <summary>
        /// retry import
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("retryimport")]
        [Authorize(Policy = RMuseumSecurableItem.ArtifactEntityShortName + ":" + RMuseumSecurableItem.ImportOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> RetryImport(JobType jobType = JobType.BritishLibrary, bool skipUpload = false)
        {
            RServiceResult<bool> res = await _artifactService.RescheduleJobs(jobType, skipUpload);
            if (res.Result)
                return Ok();
            return BadRequest(res.ExceptionString);
        }

        /// <summary>
        /// due to a bug in loc json outputs some artifacts with more than 1000 pages were downloaded incompletely
        /// </summary>
        /// <param name="pass">123456</param>
        /// <param name="skipUpload"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("reexamineimport")]
        [Authorize(Policy = RMuseumSecurableItem.ArtifactEntityShortName + ":" + RMuseumSecurableItem.ImportOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> ReExamineLocDownloads(string pass, bool skipUpload)
        {
            if (pass != "123456") //this is a heavy processor consuming call, so prevent mistaken call of it from swagger ui
                return BadRequest("invalid password");
            RServiceResult<string[]> res = await _artifactService.ReExamineLocDownloads(skipUpload);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        [HttpGet]
        [Route("jobs")]
        [Authorize(Policy = RMuseumSecurableItem.ArtifactEntityShortName + ":" + SecurableItem.AddOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<ImportJob>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetImportJobs([FromQuery] PagingParameterModel paging)
        {
            RServiceResult<(PaginationMetadata PagingMeta, ImportJob[] Items)> itemsInfo = await _artifactService.GetImportJobs(paging);
            if (!string.IsNullOrEmpty(itemsInfo.ExceptionString))
            {
                return BadRequest(itemsInfo.ExceptionString);
            }
            // Paging Header
            HttpContext.Response.Headers.Append("paging-headers", JsonConvert.SerializeObject(itemsInfo.Result.PagingMeta));

            return Ok(itemsInfo.Result.Items);
        }

        /// <summary>
        /// bookmark artifact
        /// </summary>
        /// <param name="artifactId"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("bookmark/{artifactId}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RUserBookmark))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> BookmarkArtifact(Guid artifactId)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<RUserBookmark> res = await _artifactService.BookmarkArtifact(artifactId, loggedOnUserId, RBookmarkType.Bookmark);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// bookmark item
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("bookmark/item/{itemId}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RUserBookmark))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> BookmarkItem(Guid itemId)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<RUserBookmark> res = await _artifactService.BookmarkItem(itemId, loggedOnUserId, RBookmarkType.Bookmark);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// item bookmarks info
        /// </summary>
        /// <param name="bookmarkId"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("bookmark/{bookmarkId}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> DeleteBookmark(Guid bookmarkId)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<bool> res = await _artifactService.DeleteUserBookmark(bookmarkId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok();
        }

        /// <summary>
        /// fav artifact
        /// </summary>
        /// <param name="artifactId"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("fav/{artifactId}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RUserBookmark))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> FavArtifact(Guid artifactId)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<RUserBookmark> res = await _artifactService.BookmarkArtifact(artifactId, loggedOnUserId, RBookmarkType.Favorite);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// fav item
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("fav/item/{itemId}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RUserBookmark))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> FavItem(Guid itemId)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<RUserBookmark> res = await _artifactService.BookmarkItem(itemId, loggedOnUserId, RBookmarkType.Favorite);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// artifact bookmarks info
        /// </summary>
        /// <param name="artifactId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("bookmark/{artifactId}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RUserBookmark[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GeIArtifactUserBookmarks(Guid artifactId)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<RUserBookmark[]> res = await _artifactService.GetArtifactUserBookmarks(artifactId, loggedOnUserId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// item bookmarks info
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("bookmark/item/{itemId}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RUserBookmark[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GeItemUserBookmarks(Guid itemId)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<RUserBookmark[]> res = await _artifactService.GeItemUserBookmarks(itemId, loggedOnUserId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// user bookmarks
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("bookmark")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RUserBookmark[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetUserBookmarks([FromQuery] PagingParameterModel paging)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<PublishStatus[]> v = await _GetUserVisibleArtifactStatusSet
                (
                loggedOnUserId,
                new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value)
                );
            if (!string.IsNullOrEmpty(v.ExceptionString))
                return BadRequest(v.ExceptionString);
            PublishStatus[] visibleItems = v.Result;


            RServiceResult<(PaginationMetadata PagingMeta, RUserBookmarkViewModel[] Bookmarks)> res = await _artifactService.GetBookmarks(paging, loggedOnUserId, RBookmarkType.Bookmark, visibleItems);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            // Paging Header
            HttpContext.Response.Headers.Append("paging-headers", JsonConvert.SerializeObject(res.Result.PagingMeta));
            return Ok(res.Result.Bookmarks);
        }

        /// <summary>
        /// user favorites
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("fav")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RUserBookmarkViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetUserFavorites([FromQuery] PagingParameterModel paging)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<PublishStatus[]> v = await _GetUserVisibleArtifactStatusSet
                (
                loggedOnUserId,
                new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value)
                );
            if (!string.IsNullOrEmpty(v.ExceptionString))
                return BadRequest(v.ExceptionString);
            PublishStatus[] visibleItems = v.Result;

            RServiceResult<(PaginationMetadata PagingMeta, RUserBookmarkViewModel[] Bookmarks)> res = await _artifactService.GetBookmarks(paging, loggedOnUserId, RBookmarkType.Favorite, visibleItems);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            // Paging Header
            HttpContext.Response.Headers.Append("paging-headers", JsonConvert.SerializeObject(res.Result.PagingMeta));
            return Ok(res.Result.Bookmarks);
        }

        /// <summary>
        /// add note for artifact
        /// </summary>
        /// <param name="note"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("note")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RUserNoteViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> AddUserNoteToArtifact([FromBody] PostUserNote note)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<RUserNoteViewModel> res = await _artifactService.AddUserNoteToArtifact(note.EntityId, loggedOnUserId, note.NoteType, note.Contents, note.ReferenceNoteId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);

            return Ok(res.Result);
        }

        /// <summary>
        /// add note for artifact item
        /// </summary>
        /// <param name="note"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("note/item")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RUserNoteViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> AddUserNoteToArtifactItem([FromBody] PostUserNote note)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<RUserNoteViewModel> res = await _artifactService.AddUserNoteToArtifactItem(note.EntityId, loggedOnUserId, note.NoteType, note.Contents, note.ReferenceNoteId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);

            return Ok(res.Result);
        }


        /// <summary>
        /// Edit User Notes
        /// </summary>
        /// <remarks>a note can not be edited by a user other than its owner or another using having note:moderate permission</remarks>
        /// <param name="noteId"></param>
        /// <param name="note">only htmlContent is processed</param>
        /// <returns></returns>
        [HttpPut]
        [Route("note/{noteId}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RUserNoteViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> EditUserNote(Guid noteId, [FromBody] PostUserNote note)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

            RServiceResult<bool>
                canModerate =
                await _userPermissionChecker.Check
                    (
                        loggedOnUserId,
                        new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value),
                        User.Claims.Any(c => c.Type == "Language") ? User.Claims.First(c => c.Type == "Language").Value : "fa-IR",
                        RMuseumSecurableItem.NoteEntityShortName,
                        RMuseumSecurableItem.ModerateOperationShortName
                        );
            if (!string.IsNullOrEmpty(canModerate.ExceptionString))
                return BadRequest(canModerate.ExceptionString);

            RServiceResult<RUserNoteViewModel> res = await _artifactService.EditUserNote(noteId, canModerate.Result ? (Guid?)null : loggedOnUserId, note.Contents);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);

            return Ok(res.Result);
        }

        /// <summary>
        /// delete user notes
        /// </summary>
        /// <remarks>
        /// 1. a note can not be deleted by a user other than its owner or another using having note:moderate permission
        /// 2. all notes which have refernce to deleting note (sent in reply to it) would be deleted irrelevant of their ownership
        /// </remarks>
        /// <param name="noteId"></param>
        /// <returns>list of notes deleted</returns>
        [HttpDelete]
        [Route("note/{noteId}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(Guid[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> DeleteUserNote(Guid noteId)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);

            RServiceResult<bool>
                canModerate =
                await _userPermissionChecker.Check
                    (
                        loggedOnUserId,
                        new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value),
                        User.Claims.Any(c => c.Type == "Language") ? User.Claims.First(c => c.Type == "Language").Value : "fa-IR",
                        RMuseumSecurableItem.NoteEntityShortName,
                        RMuseumSecurableItem.ModerateOperationShortName
                        );
            if (!string.IsNullOrEmpty(canModerate.ExceptionString))
                return BadRequest(canModerate.ExceptionString);

            RServiceResult<Guid[]> res = await _artifactService.DeleteUserNote(noteId, canModerate.Result ? (Guid?)null : loggedOnUserId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);

            return Ok(res.Result);
        }

        /// <summary>
        /// get private notes for artifact
        /// </summary>
        /// <param name="artifactId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("note/private/{artifactId}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RUserNoteViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetArtifactUserNotes(Guid artifactId)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<RUserNoteViewModel[]> res = await _artifactService.GetArtifactUserNotes(artifactId, loggedOnUserId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);

            return Ok(res.Result);
        }

        /// <summary>
        /// get private notes for artifact
        /// </summary>
        /// <param name="artifactId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("note/{artifactId}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RUserNoteViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetArtifactPublicNotes(Guid artifactId)
        {
            RServiceResult<RUserNoteViewModel[]> res = await _artifactService.GetArtifactPublicNotes(artifactId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);

            return Ok(res.Result);
        }

        /// <summary>
        /// get private notes for artifact item
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("note/item/private/{itemId}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RUserNoteViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetArtifactItemUserNotes(Guid itemId)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<RUserNoteViewModel[]> res = await _artifactService.GetArtifactItemUserNotes(itemId, loggedOnUserId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);

            return Ok(res.Result);
        }

        /// <summary>
        /// get public notes for artifact item
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("note/item/{itemId}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RUserNoteViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetArtifactItemPublicNotes(Guid itemId)
        {
            RServiceResult<RUserNoteViewModel[]> res = await _artifactService.GetArtifactItemPublicNotes(itemId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);

            return Ok(res.Result);
        }

        /// <summary>
        /// user public notes
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("note/public")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RUserNoteViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetUserPublicNotes([FromQuery] PagingParameterModel paging)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<PublishStatus[]> v = await _GetUserVisibleArtifactStatusSet
                (
                loggedOnUserId,
                new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value)
                );
            if (!string.IsNullOrEmpty(v.ExceptionString))
                return BadRequest(v.ExceptionString);
            PublishStatus[] visibleItems = v.Result;


            RServiceResult<(PaginationMetadata PagingMeta, RUserNoteViewModel[] Notes)> res = await _artifactService.GetUserNotes(loggedOnUserId, paging, RNoteType.Public, visibleItems);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            // Paging Header
            HttpContext.Response.Headers.Append("paging-headers", JsonConvert.SerializeObject(res.Result.PagingMeta));
            return Ok(res.Result.Notes);
        }

        /// <summary>
        /// user public notes
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("note/private")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RUserNoteViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetUserPrivateNotes([FromQuery] PagingParameterModel paging)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<PublishStatus[]> v = await _GetUserVisibleArtifactStatusSet
                (
                loggedOnUserId,
                new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value)
                );
            if (!string.IsNullOrEmpty(v.ExceptionString))
                return BadRequest(v.ExceptionString);
            PublishStatus[] visibleItems = v.Result;


            RServiceResult<(PaginationMetadata PagingMeta, RUserNoteViewModel[] Notes)> res = await _artifactService.GetUserNotes(loggedOnUserId, paging, RNoteType.Private, visibleItems);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            // Paging Header
            HttpContext.Response.Headers.Append("paging-headers", JsonConvert.SerializeObject(res.Result.PagingMeta));
            return Ok(res.Result.Notes);
        }

        /// <summary>
        /// all users public notes
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("note/all")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RUserNoteViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetAllPublicNotes([FromQuery] PagingParameterModel paging)
        {
            RServiceResult<(PaginationMetadata PagingMeta, RUserNoteViewModel[] Notes)> res = await _artifactService.GetAllPublicNotes(paging);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            // Paging Header
            HttpContext.Response.Headers.Append("paging-headers", JsonConvert.SerializeObject(res.Result.PagingMeta));
            return Ok(res.Result.Notes);
        }

        /// <summary>
        /// report a public note
        /// </summary>
        /// <param name="report"></param>
        /// <returns>id of saved report</returns>
        [HttpPost]
        [Route("note/report")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(Guid))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> ReportPublicNote([FromBody] PostRUserNoteAbuseReportViewModel report)
        {
            if (ReadOnlyMode)
                return BadRequest("سایت به دلایل فنی مثل انتقال سرور موقتاً در حالت فقط خواندنی قرار دارد. لطفاً ساعاتی دیگر مجدداً تلاش کنید.");

            Guid userId =
                 new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            Guid sessionId =
                new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value);
            RServiceResult<bool> sessionCheckResult = await _appUserService.SessionExists(userId, sessionId);
            if (!string.IsNullOrEmpty(sessionCheckResult.ExceptionString))
            {
                return StatusCode((int)HttpStatusCode.Forbidden);
            }

            RServiceResult<Guid> res = await _artifactService.ReportPublicNote(userId, report.NoteId, report.ReasonText);
            if (!string.IsNullOrEmpty(res.ExceptionString))
            {
                return BadRequest(res.ExceptionString);
            }
            return Ok(res.Result);
        }

        /// <summary>
        ///  Get a list of reported notes
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("notes/reported")]
        [Authorize(Policy = RMuseumSecurableItem.NoteEntityShortName + ":" + RMuseumSecurableItem.ModerateOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<RUserNoteAbuseReportViewModel>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetReportedPublicNotes([FromQuery] PagingParameterModel paging)
        {
            var notes = await _artifactService.GetReportedPublicNotes(paging);
            if (!string.IsNullOrEmpty(notes.ExceptionString))
            {
                return BadRequest(notes.ExceptionString);
            }

            // Paging Header
            HttpContext.Response.Headers.Append("paging-headers", JsonConvert.SerializeObject(notes.Result.PagingMeta));

            return Ok(notes.Result.Items);
        }

        /// <summary>
        /// delete a report for abuse in public user notes
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("note/report/{id}")]
        [Authorize(Policy = RMuseumSecurableItem.NoteEntityShortName + ":" + RMuseumSecurableItem.ModerateOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(int))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> DeclinePublicNoteReport(Guid id)
        {
            if (ReadOnlyMode)
                return BadRequest("سایت به دلایل فنی مثل انتقال سرور موقتاً در حالت فقط خواندنی قرار دارد. لطفاً ساعاتی دیگر مجدداً تلاش کنید.");
            RServiceResult<bool> res = await _artifactService.DeclinePublicNoteReport(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
            {
                return BadRequest(res.ExceptionString);
            }
            if (!res.Result)
            {
                return NotFound();
            }
            return Ok();
        }

        /// <summary>
        /// delete a reported user note (accept the complaint)
        /// </summary>
        /// <param name="reportid"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("note/reported/moderate/{reportid}")]
        [Authorize(Policy = RMuseumSecurableItem.NoteEntityShortName + ":" + RMuseumSecurableItem.ModerateOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> AcceptPublicNoteReport(Guid reportid)
        {
            if (ReadOnlyMode)
                return BadRequest("سایت به دلایل فنی مثل انتقال سرور موقتاً در حالت فقط خواندنی قرار دارد. لطفاً ساعاتی دیگر مجدداً تلاش کنید.");
            var res =
                await _artifactService.AcceptPublicNoteReport(reportid);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            if (!res.Result)
                return NotFound();
            return Ok();
        }



        /// <summary>
        /// suggest ganjoor link
        /// </summary>
        /// <param name="link"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("ganjoor")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorLinkViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> SuggestGanjoorLink([FromBody] LinkSuggestion link)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<GanjoorLinkViewModel> suggestion = await _artifactService.SuggestGanjoorLink(loggedOnUserId, link);
            if (!string.IsNullOrEmpty(suggestion.ExceptionString))
                return BadRequest(suggestion.ExceptionString);
            return Ok(suggestion.Result);
        }

        /// <summary>
        /// get suggested ganjoor links
        /// </summary>
        /// <param name="status"></param>
        /// <param name="notSynced"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("ganjoor")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorLinkViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetSuggestedLinks(ReviewResult status, bool notSynced)
        {
            RServiceResult<GanjoorLinkViewModel[]> res = await _artifactService.GetSuggestedLinks(status, notSynced);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// finds next unsynchronized suggested link with an aleady synched one from the artificat if exists,
        /// return value might be null or an array with length  1 or 2 (has paging-headers)
        /// </summary>
        /// <remarks>has paging-headers</remarks>
        /// <param name="skip"></param>
        /// <returns> return value might be null or an array with length  1 or 2</returns>
        [HttpGet]
        [Route("ganjoor/nextunsychedimage")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorLinkViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetNextUnsynchronizedSuggestedLinkWithAlreadySynchedOneForPoem(int skip)
        {
            RServiceResult<GanjoorLinkViewModel[]> res = await _artifactService.GetNextUnsynchronizedSuggestedLinkWithAlreadySynchedOneForPoem(skip);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            var resCount = await _artifactService.GetUnsynchronizedSuggestedLinksCount();
            if (!string.IsNullOrEmpty(resCount.ExceptionString))
                return BadRequest(resCount.ExceptionString);

            // Paging Header
            HttpContext.Response.Headers.Append("paging-headers",
                JsonConvert.SerializeObject(
                    new PaginationMetadata()
                    {
                        totalCount = resCount.Result,
                        pageSize = -1,
                        currentPage = -1,
                        hasNextPage = false,
                        hasPreviousPage = false,
                        totalPages = -1
                    })
                );
            return Ok(res.Result);
        }

        /// <summary>
        /// review suggested ganjoor link
        /// </summary>
        /// <param name="linkId"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("ganjoor/review/{linkId}/{result}")]
        [Authorize(Policy = RMuseumSecurableItem.ArtifactEntityShortName + ":" + RMuseumSecurableItem.ReviewGanjoorLinksOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> ReviewGanjoorLink(Guid linkId, ReviewResult result)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<bool> suggestion = await _artifactService.ReviewSuggestedLink(linkId, loggedOnUserId, result);
            if (!string.IsNullOrEmpty(suggestion.ExceptionString))
                return BadRequest(suggestion.ExceptionString);
            return Ok();
        }

        /// <summary>
        /// mark suggested ganjoor link as synchronized
        /// </summary>
        /// <param name="linkId"></param>
        /// <param name="displayOnPage">display ogn page</param>
        /// <returns></returns>
        [HttpPut]
        [Route("ganjoor/sync/{linkId}/{displayOnPage}")]
        [Authorize(Policy = RMuseumSecurableItem.ArtifactEntityShortName + ":" + RMuseumSecurableItem.ReviewGanjoorLinksOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> SynchronizeGanjoorLink(Guid linkId, bool displayOnPage)
        {
            RServiceResult<bool> suggestion = await _artifactService.SynchronizeSuggestedLink(linkId, displayOnPage);
            if (!string.IsNullOrEmpty(suggestion.ExceptionString))
                return BadRequest(suggestion.ExceptionString);
            return Ok();
        }

        /// <summary>
        ///toc / temporary one time api / to be removed
        /// </summary>
        /// <returns></returns>
        [HttpPut]
        [Route("ganjoor/toc")]
        [Authorize(Policy = RMuseumSecurableItem.ArtifactEntityShortName + ":" + RMuseumSecurableItem.ReviewGanjoorLinksOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> AddTOCForSuggestedLinks()
        {
            RServiceResult<string[]> suggestion = await _artifactService.AddTOCForSuggestedLinks();
            if (!string.IsNullOrEmpty(suggestion.ExceptionString))
                return BadRequest(suggestion.ExceptionString);
            return Ok(suggestion.Result);
        }

        /// <summary>
        /// get suggested pinterest links
        /// </summary>
        /// <param name="status"></param>
        /// <param name="notSynced"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("pinterest")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(PinterestLinkViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetSuggestedPinterestLinks(ReviewResult status, bool notSynced)
        {
            RServiceResult<PinterestLinkViewModel[]> res = await _artifactService.GetSuggestedPinterestLinks(status, notSynced);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// suggest pinterest link for ganjoor
        /// </summary>
        /// <param name="suggestion"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("pinterest")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(PinterestLinkViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> SuggestPinterestLink([FromBody] PinterestSuggestion suggestion)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<PinterestLinkViewModel> res = await _artifactService.SuggestPinterestLink(loggedOnUserId, suggestion);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);


            return Ok(res.Result);
        }
        /// <summary>
        /// review suggested ganjoor pinterest link
        /// </summary>
        /// <param name="linkId"></param>
        /// <param name="altText"></param>
        /// <param name="result"></param>
        /// <param name="reviewDesc"></param>
        /// <param name="imageUrl"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("pinterest/review/{linkId}/{result}")]
        [Authorize(Policy = RMuseumSecurableItem.ArtifactEntityShortName + ":" + RMuseumSecurableItem.ReviewGanjoorLinksOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> ReviewSuggestedPinterestLink(Guid linkId, string altText, ReviewResult result, string reviewDesc, string imageUrl)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<bool> suggestion = await _artifactService.ReviewSuggestedPinterestLink(linkId, loggedOnUserId, altText, result, reviewDesc, imageUrl);
            if (!string.IsNullOrEmpty(suggestion.ExceptionString))
                return BadRequest(suggestion.ExceptionString);
            return Ok();
        }

        /// <summary>
        /// mark suggested pinterest ganjoor link as synchronized
        /// </summary>
        /// <param name="linkId"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("pinterest/sync/{linkId}")]
        [Authorize(Policy = RMuseumSecurableItem.ArtifactEntityShortName + ":" + RMuseumSecurableItem.ReviewGanjoorLinksOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> SynchronizeSuggestedPinterestLink(Guid linkId)
        {
            RServiceResult<bool> suggestion = await _artifactService.SynchronizeSuggestedPinterestLink(linkId);
            if (!string.IsNullOrEmpty(suggestion.ExceptionString))
                return BadRequest(suggestion.ExceptionString);
            return Ok();
        }

        /// <summary>
        /// start filling GanjoorLink table OriginalSource values
        /// </summary>
        /// <returns></returns>
        [HttpPut("ganjoorlink/fillsource")]
        [Authorize(Policy = RMuseumSecurableItem.ArtifactEntityShortName + ":" + RMuseumSecurableItem.ReviewGanjoorLinksOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public IActionResult StartFillingGanjoorLinkOriginalSources()
        {
            try
            {
                var res = _artifactService.StartFillingGanjoorLinkOriginalSources();
                if (!string.IsNullOrEmpty(res.ExceptionString))
                    return BadRequest(res.ExceptionString);
                return Ok();
            }
            catch (Exception exp)
            {
                return BadRequest(exp.ToString());
            }
        }

        /// <summary>
        /// start removing original images
        /// </summary>
        /// <returns></returns>

        [HttpDelete("originalimages/remove")]
        [Authorize(Policy = RMuseumSecurableItem.ArtifactEntityShortName + ":" + RMuseumSecurableItem.ImportOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public IActionResult StartRemovingOriginalImages()
        {
            try
            {
                var res = _artifactService.StartRemovingOriginalImages();
                if (!string.IsNullOrEmpty(res.ExceptionString))
                    return BadRequest(res.ExceptionString);
                return Ok();
            }
            catch (Exception exp)
            {
                return BadRequest(exp.ToString());
            }
        }

        /// <summary>
        /// search artifacts
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="term"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("search")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<RArtifactMasterRecord>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]

        public async Task<IActionResult> SearchArtifacts([FromQuery] PagingParameterModel paging, string term)
        {
            var pagedResult = await _artifactService.SearchArtifacts(paging, term);
            if (!string.IsNullOrEmpty(pagedResult.ExceptionString))
                return BadRequest(pagedResult.ExceptionString);

            // Paging Header
            HttpContext.Response.Headers.Append("paging-headers", JsonConvert.SerializeObject(pagedResult.Result.PagingMeta));

            return Ok(pagedResult.Result.Items);
        }

        /// <summary>
        /// search artifact items
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="term"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("search/items")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<RArtifactItemRecordViewModel>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]

        public async Task<IActionResult> SearchArtifactItems([FromQuery] PagingParameterModel paging, string term)
        {
            var pagedResult = await _artifactService.SearchArtifactItems(paging, term);
            if (!string.IsNullOrEmpty(pagedResult.ExceptionString))
                return BadRequest(pagedResult.ExceptionString);

            // Paging Header
            HttpContext.Response.Headers.Append("paging-headers", JsonConvert.SerializeObject(pagedResult.Result.PagingMeta));

            return Ok(pagedResult.Result.Items);
        }

        /// <summary>
        /// start setting an artifact items as a category poems text original source
        /// </summary>
        /// <param name="ganjoorCatId"></param>
        /// <param name="artifactId"></param>
        /// <returns></returns>

        [HttpPut("ganjoorlink/settextorigin/{ganjoorCatId}/{artifactId}")]
        [Authorize(Policy = RMuseumSecurableItem.ArtifactEntityShortName + ":" + RMuseumSecurableItem.ReviewGanjoorLinksOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public IActionResult StartSettingArtifactAsTextOriginalSource(int ganjoorCatId, Guid artifactId)
        {
            try
            {
                var res = _artifactService.StartSettingArtifactAsTextOriginalSource(ganjoorCatId, artifactId);
                if (!string.IsNullOrEmpty(res.ExceptionString))
                    return BadRequest(res.ExceptionString);
                return Ok();
            }
            catch (Exception exp)
            {
                return BadRequest(exp.ToString());
            }
        }

        /// <summary>
        /// upload artifact to external server
        /// </summary>
        /// <param name="artifactId"></param>
        /// <param name="skipUpload"></param>
        /// <returns></returns>

        [HttpPut("upload/external/{artifactId}")]
        [Authorize(Policy = RMuseumSecurableItem.ArtifactEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public IActionResult StartUploadingArtifactToExternalServer(Guid artifactId, bool skipUpload)
        {
            try
            {
                var res = _artifactService.StartUploadingArtifactToExternalServer(artifactId, skipUpload);
                if (!string.IsNullOrEmpty(res.ExceptionString))
                    return BadRequest(res.ExceptionString);
                return Ok();
            }
            catch (Exception exp)
            {
                return BadRequest(exp.ToString());
            }
        }

        /// <summary>
        /// readonly mode
        /// </summary>
        public bool ReadOnlyMode
        {
            get
            {
                try
                {
                    return bool.Parse(Configuration["ReadOnlyMode"]);
                }
                catch
                {
                    return false;
                }
            }
        }



        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="artifactService"></param>
        /// <param name="userPermissionChecker"></param>
        /// <param name="memoryCache"></param>
        /// <param name="captchaService"></param>
        /// <param name="appUserService"></param>
        /// <param name="configuration"></param>
        public ArtifactController(IArtifactService artifactService, IUserPermissionChecker userPermissionChecker, IMemoryCache memoryCache, ICaptchaService captchaService, IAppUserService appUserService, IConfiguration configuration)
        {
            _artifactService = artifactService;
            _userPermissionChecker = userPermissionChecker;
            _memoryCache = memoryCache;
            _captchaService = captchaService;
            _appUserService = appUserService;
            Configuration = configuration;

        }

        /// <summary>
        /// Artifact Service
        /// </summary>
        protected readonly IArtifactService _artifactService;

        /// <summary>
        /// IUserPermissionChecker instance
        /// </summary>
        protected IUserPermissionChecker _userPermissionChecker;

        /// <summary>
        /// IMemoryCache
        /// </summary>
        protected readonly IMemoryCache _memoryCache;

        /// <summary>
        /// Captcha service
        /// </summary>
        protected readonly ICaptchaService _captchaService;

        /// <summary>
        /// IAppUserService instance
        /// </summary>
        protected IAppUserService _appUserService;

        /// <summary>
        /// Configuration
        /// </summary>
        protected IConfiguration Configuration { get; }


    }
}
