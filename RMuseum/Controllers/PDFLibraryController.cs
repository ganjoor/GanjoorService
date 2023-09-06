using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RMuseum.Models.Auth.Memory;
using RMuseum.Models.PDFLibrary;
using RMuseum.Models.PDFLibrary.ViewModels;
using RMuseum.Services;
using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Models.Generic;
using System.Linq;
using System;
using System.Net;
using System.Threading.Tasks;
using RSecurityBackend.Services;
using System.Collections.Generic;
using Newtonsoft.Json;
using RMuseum.Models.Artifact;
using RMuseum.Models.GanjoorIntegration.ViewModels;
using RMuseum.Models.GanjoorIntegration;

namespace RMuseum.Controllers
{
    [Produces("application/json")]
    [Route("api/pdf")]
    public class PDFLibraryController : Controller
    {
        /// <summary>
        ///get all published pdfbooks (including CoverImage info but not pages or tagibutes info) - check paging-headers for paging info
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<PDFBook>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]

        public async Task<IActionResult> GetAllPDFBooksAsync([FromQuery] PagingParameterModel paging)
        {
            var pdfBooksInfo = await _pdfService.GetAllPDFBooksAsync(paging, new PublishStatus[] { PublishStatus.Published });
            if (!string.IsNullOrEmpty(pdfBooksInfo.ExceptionString))
            {
                return BadRequest(pdfBooksInfo.ExceptionString);
            }

            if (pdfBooksInfo.Result.Books.Count() > 0)
            {
                DateTime lastModification = pdfBooksInfo.Result.Books.Max(i => i.LastModified);
                Response.GetTypedHeaders().LastModified = lastModification;

                var requestHeaders = Request.GetTypedHeaders();
                if (requestHeaders.IfModifiedSince.HasValue &&
                    requestHeaders.IfModifiedSince.Value >= lastModification)
                {
                    return StatusCode(StatusCodes.Status304NotModified);
                }
            }

            // Paging Header
            HttpContext.Response.Headers.Add("paging-headers", JsonConvert.SerializeObject(pdfBooksInfo.Result.PagingMeta));

            return Ok(pdfBooksInfo.Result.Books);
        }

        /// <summary>
        /// get all pdf books visible by user (including CoverImage info but not items info)
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("secure")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<PDFBook>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetUserVisiblePDFBooksAsync([FromQuery] PagingParameterModel paging)
        {
            RServiceResult<PublishStatus[]> v = await _GetUserVisiblePDFBooksStatusSetAsync
                (
                new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value),
                new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value)
                );
            if (!string.IsNullOrEmpty(v.ExceptionString))
                return BadRequest(v.ExceptionString);
            PublishStatus[] visibleItems = v.Result;

            if (visibleItems.Length == 1 && visibleItems[0] == PublishStatus.Published) //Caching
            {
                return await GetAllPDFBooksAsync(paging);
            }

            RServiceResult<(PaginationMetadata PagingMeta, PDFBook[] Books)> itemsInfo = await _pdfService.GetAllPDFBooksAsync(paging, visibleItems);
            if (!string.IsNullOrEmpty(itemsInfo.ExceptionString))
            {
                return BadRequest(itemsInfo.ExceptionString);
            }

            if (itemsInfo.Result.Books.Count() > 0)
            {
                DateTime lastModification = itemsInfo.Result.Books.Max(i => i.LastModified);
                Response.GetTypedHeaders().LastModified = lastModification;

                var requestHeaders = Request.GetTypedHeaders();
                if (requestHeaders.IfModifiedSince.HasValue &&
                    requestHeaders.IfModifiedSince.Value >= lastModification)
                {
                    return StatusCode(StatusCodes.Status304NotModified);
                }
            }

            // Paging Header
            HttpContext.Response.Headers.Add("paging-headers", JsonConvert.SerializeObject(itemsInfo.Result.PagingMeta));

            return Ok(itemsInfo.Result.Books);
        }

        /// <summary>
        /// secure get a pdf book
        /// </summary>
        /// <param name="id"></param>
        /// <param name="includePages"></param>
        /// <param name="includeBookText"></param>
        /// <param name="includePageText"></param>
        /// <returns></returns>
        [HttpGet("secure/{id}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(PDFBook))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetUserVisiblePDFBookAsync(int id, bool includePages = false, bool includeBookText = false, bool includePageText = false)
        {
            RServiceResult<PublishStatus[]> v = await _GetUserVisiblePDFBooksStatusSetAsync
               (
               new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value),
               new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value)
               );
            if (!string.IsNullOrEmpty(v.ExceptionString))
                return BadRequest(v.ExceptionString);
            PublishStatus[] visibleItems = v.Result;
            RServiceResult<PDFBook> bookRes = null;
            if (visibleItems.Length == 1 && visibleItems[0] == PublishStatus.Published)
            {
                bookRes = await _pdfService.GetPDFBookByIdAsync(id, new PublishStatus[] { PublishStatus.Published }, includePages, includeBookText, includePageText);
                if (!string.IsNullOrEmpty(bookRes.ExceptionString))
                {
                    return BadRequest(bookRes.ExceptionString);
                }
                if (bookRes.Result == null)
                    return NotFound();
            }
            if (bookRes == null)
            {
                bookRes = await _pdfService.GetPDFBookByIdAsync(id, visibleItems, includePages, includeBookText, includePageText);
            }

            if (!string.IsNullOrEmpty(bookRes.ExceptionString))
            {
                return BadRequest(bookRes.ExceptionString);
            }
            if (bookRes.Result == null)
                return NotFound();

            Response.GetTypedHeaders().LastModified = bookRes.Result.LastModified;

            var requestHeaders = Request.GetTypedHeaders();
            if (requestHeaders.IfModifiedSince.HasValue &&
                requestHeaders.IfModifiedSince.Value >= bookRes.Result.LastModified)
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }


            return Ok(bookRes.Result);
        }

        private async Task<RServiceResult<PublishStatus[]>> _GetUserVisiblePDFBooksStatusSetAsync(Guid loggedOnUserId, Guid sessionId)
        {
            RServiceResult<bool>
                canView =
                await _userPermissionChecker.Check
                    (
                        loggedOnUserId,
                        sessionId,
                        RMuseumSecurableItem.PDFLibraryEntityShortName,
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
        /// get published PDF Book by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="includePages"></param>
        /// <param name="includeBookText"></param>
        /// <param name="includePageText"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(PDFBook))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetPDFBookByIdAsync(int id, bool includePages = false, bool includeBookText = false, bool includePageText = false)
        {
            var bookRes = await _pdfService.GetPDFBookByIdAsync(id, new PublishStatus[] { PublishStatus.Published }, includePages, includeBookText, includePageText);

            if (!string.IsNullOrEmpty(bookRes.ExceptionString))
            {
                return BadRequest(bookRes.ExceptionString);
            }
            if (bookRes.Result == null)
                return NotFound();

            Response.GetTypedHeaders().LastModified = bookRes.Result.LastModified;

            var requestHeaders = Request.GetTypedHeaders();
            if (requestHeaders.IfModifiedSince.HasValue &&
                requestHeaders.IfModifiedSince.Value >= bookRes.Result.LastModified)
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }


            return Ok(bookRes.Result);
        }


        /// <summary>
        /// start importing a local pdf file
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + SecurableItem.AddOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> StartImportingLocalPDFAsync([FromBody] NewPDFBookViewModel model)
        {
            var res = await _pdfService.StartImportingLocalPDFAsync(model);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok();
        }
        /// <summary>
        /// import from known sources
        /// </summary>
        /// <param name="srcUrl"></param>
        /// <returns></returns>

        [HttpPost("import")]
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + SecurableItem.AddOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public IActionResult StartImportingKnownSourceAsync([FromBody]string srcUrl)
        {
            _pdfService.StartImportingKnownSourceAsync(srcUrl);
            return Ok();
        }

        /// <summary>
        /// batch import soha library
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="finalizeDownload"></param>
        /// <returns></returns>
        [HttpPost("soha/{start}/{end}")]
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + SecurableItem.AddOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public IActionResult BatchImportSohaLibraryAsync(int start, int end, bool finalizeDownload)
        {
            _pdfService.BatchImportSohaLibraryAsync(start, end, finalizeDownload);
            return Ok();
        }

        
        /// <summary>
        /// batch import eliteraturebook.com library
        /// </summary>
        /// <param name="ajaxPageIndexStart">start from 0</param>
        /// <param name="ajaxPageIndexEnd"></param>
        /// <param name="finalizeDownload"></param>
        /// <returns></returns>
        [HttpPost("elit/{ajaxPageIndexStart}/{ajaxPageIndexEnd}")]
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + SecurableItem.AddOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public IActionResult BatchImportELiteratureBookLibraryAsync(int ajaxPageIndexStart, int ajaxPageIndexEnd, bool finalizeDownload)
        {
            _pdfService.BatchImportELiteratureBookLibraryAsync(ajaxPageIndexStart, ajaxPageIndexEnd, finalizeDownload);
            return Ok();
        }


        /// <summary>
        /// edit pdf book master record (user should have additional permissions pdf:awaiting and pdf:publish to change status of pdf book)
        /// </summary>
        /// <remarks>
        /// editing related collections such as pages and attributed or complex properties such as CoverImage is ignored
        /// </remarks>
        /// <param name="pdf"></param>
        /// <returns></returns>
        [HttpPut]
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> PutPDFBookAsync([FromBody] PDFBook pdf)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            Guid sessionId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "SessionId").Value);

            RServiceResult<bool>
                canChangeStatusToAwaiting =
                await _userPermissionChecker.Check
                    (
                        loggedOnUserId,
                        sessionId,
                        RMuseumSecurableItem.PDFLibraryEntityShortName,
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
                        RMuseumSecurableItem.PDFLibraryEntityShortName,
                        RMuseumSecurableItem.PublishOperationShortName
                        );
            if (!string.IsNullOrEmpty(canPublish.ExceptionString))
                return BadRequest(canPublish.ExceptionString);

            RServiceResult<PDFBook> itemInfo = await _pdfService.EditPDFBookMasterRecordAsync(pdf, canChangeStatusToAwaiting.Result, canPublish.Result);
            if (!string.IsNullOrEmpty(itemInfo.ExceptionString))
            {
                return BadRequest(itemInfo.ExceptionString);
            }

            if (itemInfo == null)
            {
                return NotFound();
            }

            return Ok();
        }

        /// <summary>
        /// Copy PDF Book Cover Image From Page Thumbnail image
        /// </summary>
        /// <param name="id"></param>
        /// <param name="pageId"></param>
        /// <returns></returns>
        [HttpPut("{id}/cover/{pageId}")]
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> SetPDFBookCoverImageFromPageAsync(int id, int pageId)
        {
            RServiceResult<bool> res = await _pdfService.SetPDFBookCoverImageFromPageAsync(id, pageId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest();
            return Ok();
        }

        /// <summary>
        /// remove unpublished pdf book
        /// </summary>
        /// <param name="bookId"></param>
        /// <returns></returns>
        [HttpDelete("{bookId}")]
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + SecurableItem.DeleteOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> RemovePDFBookAsync(int bookId)
        {
            RServiceResult<bool> res = await _pdfService.RemovePDFBookAsync(bookId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
            {
                return BadRequest(res.ExceptionString);
            }
            return Ok(res.Result);
        }

        /// <summary>
        /// add new tag value to pdf book
        /// </summary>
        /// <param name="pdfBookId"></param>
        /// <param name="tag">only name is processed</param>
        /// <returns></returns>
        [HttpPost("tagvalue/{pdfBookId}")]
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + RMuseumSecurableItem.EditTagValueOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(RTagValue))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> TagPDFBookAsync(int pdfBookId, [FromBody] RTag tag)
        {
            RServiceResult<RTagValue> res = await _pdfService.TagPDFBookAsync(pdfBookId, tag);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// edit pdf book attribute value
        /// </summary>
        /// <remarks>
        /// editable fields are limited
        /// </remarks>
        /// <param name="pdfBookId"></param>
        /// <param name="tagvalue"></param>
        /// <param name="global">apply on all same value tags</param>
        /// <returns></returns>
        [HttpPut("tagvalue/{pdfBookId}/{global=true}")]
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + RMuseumSecurableItem.EditTagValueOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> EditPDFBookTagValueAsync(int pdfBookId, bool global, [FromBody] RTagValue tagvalue)
        {

            RServiceResult<RTagValue> itemInfo = await _pdfService.EditPDFBookTagValueAsync(pdfBookId, tagvalue, global);
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
        /// remove tag from pdf book
        /// </summary>
        /// <param name="pdfBookId"></param>
        /// <param name="tagValueId"></param>
        /// <returns></returns>
        [HttpDelete("tagvalue/{pdfBookId}/{tagValueId}")]
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + RMuseumSecurableItem.EditTagValueOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> UnTagPDFBookAsync(int pdfBookId, Guid tagValueId)
        {
            RServiceResult<bool> res = await _pdfService.UnTagPDFBookAsync(pdfBookId, tagValueId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
            {
                return BadRequest(res.ExceptionString);
            }
            return Ok(res.Result);
        }

        /// <summary>
        /// get tagged publish pdfbooks (including CoverImage info but not pages or tagibutes info) 
        /// </summary>
        /// <param name="tagUrl"></param>
        /// <param name="valueUrl"></param>
        /// <returns></returns>

        [HttpGet("tagged/{tagUrl}/{valueUrl}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<PDFBook>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetByTagValueAsync(string tagUrl, string valueUrl)
        {
            RServiceResult<PDFBook[]> itemsInfo = await _pdfService.GetPDFBookByTagValueAsync(tagUrl, valueUrl, new PublishStatus[] { PublishStatus.Published });
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
        /// get authors
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="authorName"></param>
        /// <returns></returns>
        [HttpGet("author")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<Author>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]

        public async Task<IActionResult> GetAuthorsAsync([FromQuery] PagingParameterModel paging, string authorName = null)
        {
            var authorsRes = await _pdfService.GetAuthorsAsync(paging, authorName);
            if (!string.IsNullOrEmpty(authorsRes.ExceptionString))
            {
                return BadRequest(authorsRes.ExceptionString);
            }

            if (authorsRes.Result.Authors.Count() > 0)
            {
                DateTime lastModification = authorsRes.Result.Authors.Max(i => i.LastModified);
                Response.GetTypedHeaders().LastModified = lastModification;

                var requestHeaders = Request.GetTypedHeaders();
                if (requestHeaders.IfModifiedSince.HasValue &&
                    requestHeaders.IfModifiedSince.Value >= lastModification)
                {
                    return StatusCode(StatusCodes.Status304NotModified);
                }
            }

            // Paging Header
            HttpContext.Response.Headers.Add("paging-headers", JsonConvert.SerializeObject(authorsRes.Result.PagingMeta));

            return Ok(authorsRes.Result.Authors);
        }

        /// <summary>
        /// get author by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("author/{id}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(Author))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]

        public async Task<IActionResult> GetAuthorByIdAsync(int id)
        {
            var authorsRes = await _pdfService.GetAuthorByIdAsync(id);
            if (!string.IsNullOrEmpty(authorsRes.ExceptionString))
            {
                return BadRequest(authorsRes.ExceptionString);
            }

            if(authorsRes.Result == null)
            {
                return NotFound();
            }

            return Ok(authorsRes.Result);
        }

        /// <summary>
        /// add a new author
        /// </summary>
        /// <param name="author"></param>
        /// <returns></returns>

        [HttpPost("author")]
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + SecurableItem.AddOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(Author))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> AddAuthorAsync([FromBody] Author author)
        {
            var res = await _pdfService.AddAuthorAsync(author);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// edit an existing author
        /// </summary>
        /// <param name="author"></param>
        /// <returns></returns>

        [HttpPut("author")]
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> UpdateAuthorAsync([FromBody] Author author)
        {
            var res = await _pdfService.UpdateAuthorAsync(author);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok();
        }

        /// <summary>
        /// delete an existing author
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>

        [HttpDelete("author/{id}")]
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + SecurableItem.DeleteOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> DeleteAuthorAsync(int id)
        {
            var res = await _pdfService.DeleteAuthorAsync(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok();
        }

        [HttpPost("pdfbook/{pdfBookId}/contributor")]
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> AddPDFBookContributerAsync(int pdfBookId, [FromBody] AuthorRole role)
        {
            var res = await _pdfService.AddPDFBookContributerAsync(pdfBookId, role.Author.Id, role.Role);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok();
        }


        /// <summary>
        /// delete an existing contribution from pdf book
        /// </summary>
        /// <param name="pdfBookId"></param>
        /// <param name="contributorRecordId"></param>
        /// <returns></returns>

        [HttpDelete("pdfbook/{pdfBookId}/contributor/{contributorRecordId}")]
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> DeletePDFBookContributerAsync(int pdfBookId, int contributorRecordId)
        {
            var res = await _pdfService.DeletePDFBookContributerAsync(pdfBookId, contributorRecordId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok();
        }

        /// <summary>
        /// pdf book by contributer
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="authorId"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        [HttpGet("pdfbook/by/contributer/{authorId}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<PDFBook>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]

        public async Task<IActionResult> GetPublishedPDFBooksByAuthorAsync([FromQuery] PagingParameterModel paging, int authorId, string role = null)
        {
            var pdfBooksInfo = await _pdfService.GetPublishedPDFBooksByAuthorAsync(paging, authorId, role);
            if (!string.IsNullOrEmpty(pdfBooksInfo.ExceptionString))
            {
                return BadRequest(pdfBooksInfo.ExceptionString);
            }

            if (pdfBooksInfo.Result.Books.Count() > 0)
            {
                DateTime lastModification = pdfBooksInfo.Result.Books.Max(i => i.LastModified);
                Response.GetTypedHeaders().LastModified = lastModification;

                var requestHeaders = Request.GetTypedHeaders();
                if (requestHeaders.IfModifiedSince.HasValue &&
                    requestHeaders.IfModifiedSince.Value >= lastModification)
                {
                    return StatusCode(StatusCodes.Status304NotModified);
                }
            }

            // Paging Header
            HttpContext.Response.Headers.Add("paging-headers", JsonConvert.SerializeObject(pdfBooksInfo.Result.PagingMeta));

            return Ok(pdfBooksInfo.Result.Books);
        }

        /// <summary>
        /// get published pdf books by author stats (group by role)
        /// </summary>
        /// <param name="authorId"></param>
        /// <returns></returns>
        [HttpGet("pdfbook/by/contributer/{authorId}/groupby/role")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<AuthorRoleCount>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]

        public async Task<IActionResult> GetPublishedPDFBookbyAuthorGroupedByRoleAsync(int authorId)
        {
            var res = await _pdfService.GetPublishedPDFBookbyAuthorGroupedByRoleAsync(authorId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
            {
                return BadRequest(res.ExceptionString);
            }

            return Ok(res.Result);
        }

        /// <summary>
        /// get all books
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>

        [HttpGet("book")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<Book>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]

        public async Task<IActionResult> GetAllBooksAsync([FromQuery] PagingParameterModel paging)
        {
            var res = await _pdfService.GetAllBooksAsync(paging);
            if (!string.IsNullOrEmpty(res.ExceptionString))
            {
                return BadRequest(res.ExceptionString);
            }

            if (res.Result.Books.Count() > 0)
            {
                DateTime lastModification = res.Result.Books.Max(i => i.LastModified);
                Response.GetTypedHeaders().LastModified = lastModification;

                var requestHeaders = Request.GetTypedHeaders();
                if (requestHeaders.IfModifiedSince.HasValue &&
                    requestHeaders.IfModifiedSince.Value >= lastModification)
                {
                    return StatusCode(StatusCodes.Status304NotModified);
                }
            }

            // Paging Header
            HttpContext.Response.Headers.Add("paging-headers", JsonConvert.SerializeObject(res.Result.PagingMeta));

            return Ok(res.Result.Books);
        }

        /// <summary>
        /// book by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("book/{id}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(Book))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]

        public async Task<IActionResult> GetBookByIdAsync(int id)
        {
            var res = await _pdfService.GetBookByIdAsync(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
            {
                return BadRequest(res.ExceptionString);
            }

            return Ok(res.Result);
        }

        /// <summary>
        /// add a new book
        /// </summary>
        /// <param name="book"></param>
        /// <returns></returns>

        [HttpPost("book")]
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + SecurableItem.AddOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(Book))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> AddBookAsync([FromBody] Book book)
        {
            var res = await _pdfService.AddBookAsync(book);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// update book
        /// </summary>
        /// <param name="book"></param>
        /// <returns></returns>
        [HttpPut("book")]
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> UpdateBookAsync([FromBody] Book book)
        {
            var res = await _pdfService.UpdateBookAsync(book);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok();
        }

        /// <summary>
        /// delete book
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("book")]
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + SecurableItem.DeleteOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> DeleteBookAsync(int id)
        {
            var res = await _pdfService.DeleteBookAsync(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok();
        }

        /// <summary>
        /// add book author
        /// </summary>
        /// <param name="bookId"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        [HttpPost("book/{bookId}/author")]
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> AddBookAuthorAsync(int bookId, [FromBody] AuthorRole role)
        {
            var res = await _pdfService.AddBookAuthorAsync(bookId, role.Author.Id, role.Role);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok();
        }


        /// <summary>
        /// delete an existing author from book
        /// </summary>
        /// <param name="bookId"></param>
        /// <param name="authorRecordId"></param>
        /// <returns></returns>

        [HttpDelete("book/{bookId}/author/{authorRecordId}")]
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> DeleteBookAuthorAsync(int bookId, int authorRecordId)
        {
            var res = await _pdfService.DeleteBookAuthorAsync(bookId, authorRecordId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok();
        }

        /// <summary>
        /// books by author
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="authorId"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        [HttpGet("book/by/author/{authorId}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<Book>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]

        public async Task<IActionResult> GetBooksByAuthorAsync([FromQuery] PagingParameterModel paging, int authorId, string role = null)
        {
            var res = await _pdfService.GetBooksByAuthorAsync(paging, authorId, role);
            if (!string.IsNullOrEmpty(res.ExceptionString))
            {
                return BadRequest(res.ExceptionString);
            }

            if (res.Result.Books.Count() > 0)
            {
                DateTime lastModification = res.Result.Books.Max(i => i.LastModified);
                Response.GetTypedHeaders().LastModified = lastModification;

                var requestHeaders = Request.GetTypedHeaders();
                if (requestHeaders.IfModifiedSince.HasValue &&
                    requestHeaders.IfModifiedSince.Value >= lastModification)
                {
                    return StatusCode(StatusCodes.Status304NotModified);
                }
            }

            // Paging Header
            HttpContext.Response.Headers.Add("paging-headers", JsonConvert.SerializeObject(res.Result.PagingMeta));

            return Ok(res.Result.Books);
        }

        /// <summary>
        /// get books by author stats (group by role)
        /// </summary>
        /// <param name="authorId"></param>
        /// <returns></returns>
        [HttpGet("book/by/author/{authorId}/groupby/role")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<AuthorRoleCount>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]

        public async Task<IActionResult> GetBookbyAuthorGroupedByRoleAsync(int authorId)
        {
            var res = await _pdfService.GetBookbyAuthorGroupedByRoleAsync(authorId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
            {
                return BadRequest(res.ExceptionString);
            }

            return Ok(res.Result);
        }

        /// <summary>
        /// get book related pdf books
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="sourceId"></param>
        /// <returns></returns>

        [HttpGet("book/{bookId}/pdfs")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<PDFBook>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]

        public async Task<IActionResult> GetBookRelatedPDFBooksAsync([FromQuery] PagingParameterModel paging, int sourceId)
        {
            var res = await _pdfService.GetBookRelatedPDFBooksAsync(paging, sourceId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
            {
                return BadRequest(res.ExceptionString);
            }

            if (res.Result.Books.Count() > 0)
            {
                DateTime lastModification = res.Result.Books.Max(i => i.LastModified);
                Response.GetTypedHeaders().LastModified = lastModification;

                var requestHeaders = Request.GetTypedHeaders();
                if (requestHeaders.IfModifiedSince.HasValue &&
                    requestHeaders.IfModifiedSince.Value >= lastModification)
                {
                    return StatusCode(StatusCodes.Status304NotModified);
                }
            }

            // Paging Header
            HttpContext.Response.Headers.Add("paging-headers", JsonConvert.SerializeObject(res.Result.PagingMeta));

            return Ok(res.Result.Books);
        }

        /// <summary>
        /// volumes by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>

        [HttpGet("volumes/{id}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(MultiVolumePDFCollection))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]

        public async Task<IActionResult> GetMultiVolumePDFCollectionByIdAsync(int id)
        {
            var res = await _pdfService.GetMultiVolumePDFCollectionByIdAsync(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
            {
                return BadRequest(res.ExceptionString);
            }

            return Ok(res.Result);
        }


        /// <summary>
        /// add a new multi volume pdf collection
        /// </summary>
        /// <param name="volumes"></param>
        /// <returns></returns>
        [HttpPost("volumes")]
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + SecurableItem.AddOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(MultiVolumePDFCollection))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> AddMultiVolumePDFCollectionAsync([FromBody] MultiVolumePDFCollection volumes)
        {
            var res = await _pdfService.AddMultiVolumePDFCollectionAsync(volumes);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// update multi volume pdf collection
        /// </summary>
        /// <param name="volumes"></param>
        /// <returns></returns>

        [HttpPut("volumes")]
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> UpdateMultiVolumePDFCollectionAsync([FromBody] MultiVolumePDFCollection volumes)
        {
            var res = await _pdfService.UpdateMultiVolumePDFCollectionAsync(volumes);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// delete multi volume pdf collection
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("volumes")]
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + SecurableItem.DeleteOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> DeleteMultiVolumePDFCollectionAsync(int id)
        {
            var res = await _pdfService.DeleteMultiVolumePDFCollectionAsync(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }
        /// <summary>
        /// get volumes pdf books
        /// </summary>
        /// <param name="volumeId"></param>
        /// <returns></returns>
        [HttpGet("volumes/{volumeId}/pdfs")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<PDFBook>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]

        public async Task<IActionResult> GetVolumesPDFBooks(int volumeId)
        {
            var res = await _pdfService.GetVolumesPDFBooks(volumeId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
            {
                return BadRequest(res.ExceptionString);
            }

            return Ok(res.Result);
        }

        /// <summary>
        /// get all sources
        /// </summary>
        /// <returns></returns>

        [HttpGet("source")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<PDFSource>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]

        public async Task<IActionResult> GetPDFSourcesAsync()
        {
            var res = await _pdfService.GetPDFSourcesAsync();
            if (!string.IsNullOrEmpty(res.ExceptionString))
            {
                return BadRequest(res.ExceptionString);
            }

            return Ok(res.Result);
        }

        /// <summary>
        /// source by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("source/{id}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(PDFSource))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]

        public async Task<IActionResult> GetPDFSourceByIdAsync(int id)
        {
            var res = await _pdfService.GetPDFSourceByIdAsync(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
            {
                return BadRequest(res.ExceptionString);
            }

            return Ok(res.Result);
        }

        /// <summary>
        /// add a new source
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>

        [HttpPost("source")]
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + SecurableItem.AddOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(PDFSource))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> AddPDFSourceAsync([FromBody] PDFSource source)
        {
            var res = await _pdfService.AddPDFSourceAsync(source);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// update source
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        [HttpPut("source")]
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> UpdatePDFSourceAsync([FromBody] PDFSource source)
        {
            var res = await _pdfService.UpdatePDFSourceAsync(source);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok();
        }

        /// <summary>
        /// delete book
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("source")]
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + SecurableItem.DeleteOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> DeletePDFSourceAsync(int id)
        {
            var res = await _pdfService.DeletePDFSourceAsync(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok();
        }

        /// <summary>
        /// get pdf source pdfs
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="sourceId"></param>
        /// <returns></returns>
        [HttpGet("source/{sourceId}/pdfs")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<PDFBook>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]

        public async Task<IActionResult> GetSourceRelatedPDFBooksAsync([FromQuery] PagingParameterModel paging, int sourceId)
        {
            var res = await _pdfService.GetSourceRelatedPDFBooksAsync(paging, sourceId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
            {
                return BadRequest(res.ExceptionString);
            }

            if (res.Result.Books.Count() > 0)
            {
                DateTime lastModification = res.Result.Books.Max(i => i.LastModified);
                Response.GetTypedHeaders().LastModified = lastModification;

                var requestHeaders = Request.GetTypedHeaders();
                if (requestHeaders.IfModifiedSince.HasValue &&
                    requestHeaders.IfModifiedSince.Value >= lastModification)
                {
                    return StatusCode(StatusCodes.Status304NotModified);
                }
            }

            // Paging Header
            HttpContext.Response.Headers.Add("paging-headers", JsonConvert.SerializeObject(res.Result.PagingMeta));

            return Ok(res.Result.Books);
        }

        /// <summary>
        /// search pdf books (titles and authors and translators and tags)
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="term"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("search")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<RArtifactMasterRecord>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]

        public async Task<IActionResult> SearchPDFBooksAsync([FromQuery] PagingParameterModel paging, string term)
        {
            var pagedResult = await _pdfService.SearchPDFBooksAsync(paging, term);
            if (!string.IsNullOrEmpty(pagedResult.ExceptionString))
                return BadRequest(pagedResult.ExceptionString);

            // Paging Header
            HttpContext.Response.Headers.Add("paging-headers", JsonConvert.SerializeObject(pagedResult.Result.PagingMeta));

            return Ok(pagedResult.Result.Items);
        }

        /// <summary>
        /// search pdf book pages text
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="id"></param>
        /// <param name="term"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("search/pdfbook/{id}/text")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<RArtifactMasterRecord>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]

        public async Task<IActionResult> SearchPDFPagesTextAsync([FromQuery] PagingParameterModel paging, int id, string term)
        {
            var pagedResult = await _pdfService.SearchPDFPagesTextAsync(paging, id, term);
            if (!string.IsNullOrEmpty(pagedResult.ExceptionString))
                return BadRequest(pagedResult.ExceptionString);

            // Paging Header
            HttpContext.Response.Headers.Add("paging-headers", JsonConvert.SerializeObject(pagedResult.Result.PagingMeta));

            return Ok(pagedResult.Result.Items);
        }

        /// <summary>
        /// search book text
        /// </summary>
        /// <param name="paging"></param>
        /// <param name="term"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("search/pages/text")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<RArtifactMasterRecord>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]

        public async Task<IActionResult> SearchPDFBookForPDFPagesTextAsync([FromQuery] PagingParameterModel paging, string term)
        {
            var pagedResult = await _pdfService.SearchPDFBookForPDFPagesTextAsync(paging, term);
            if (!string.IsNullOrEmpty(pagedResult.ExceptionString))
                return BadRequest(pagedResult.ExceptionString);

            // Paging Header
            HttpContext.Response.Headers.Add("paging-headers", JsonConvert.SerializeObject(pagedResult.Result.PagingMeta));

            return Ok(pagedResult.Result.Books);
        }

        /// <summary>
        /// suggest ganjoor link
        /// </summary>
        /// <param name="link"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("ganjoor")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> SuggestGanjoorLinkAsync([FromBody] PDFGanjoorLinkSuggestion link)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<bool> suggestion = await _pdfService.SuggestGanjoorLinkAsync(loggedOnUserId, link);
            if (!string.IsNullOrEmpty(suggestion.ExceptionString))
                return BadRequest(suggestion.ExceptionString);
            return Ok();
        }

        /// <summary>
        /// finds next awaiting suggested link 
        /// return value might be null (has paging-headers)
        /// </summary>
        /// <remarks>has paging-headers</remarks>
        /// <param name="skip"></param>
        /// <returns> return value might be null </returns>
        [HttpGet]
        [Route("ganjoor/nextunreviewed")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorLinkViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetNextUnreviewedGanjoorLinkAsync(int skip)
        {
            RServiceResult<GanjoorLinkViewModel> res = await _pdfService.GetNextUnreviewedGanjoorLinkAsync(skip);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            var resCount = await _pdfService.GetUnreviewedGanjoorLinksCountAsync();
            if (!string.IsNullOrEmpty(resCount.ExceptionString))
                return BadRequest(resCount.ExceptionString);

            // Paging Header
            HttpContext.Response.Headers.Add("paging-headers",
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
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + RMuseumSecurableItem.ReviewGanjoorLinksOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> ReviewSuggestedLinkAsync(Guid linkId, ReviewResult result)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<bool> suggestion = await _pdfService.ReviewSuggestedLinkAsync(linkId, loggedOnUserId, result);
            if (!string.IsNullOrEmpty(suggestion.ExceptionString))
                return BadRequest(suggestion.ExceptionString);
            return Ok();
        }

        /// <summary>
        /// ganjoor approved unsycned links
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("ganjoor/unsynched")]
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + RMuseumSecurableItem.ReviewGanjoorLinksOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(PDFGanjoorLink[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetUnsyncedPDFGanjoorLinksAsync()
        {
            RServiceResult<PDFGanjoorLink[]> res = await _pdfService.GetUnsyncedPDFGanjoorLinksAsync();
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// synchronize ganjoor link
        /// </summary>
        /// <param name="linkId"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("ganjoor/sync/{linkId}")]
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + RMuseumSecurableItem.ReviewGanjoorLinksOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> SynchronizePDFGanjoorLinkAsync(Guid linkId)
        {
            Guid loggedOnUserId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            RServiceResult<bool> suggestion = await _pdfService.SynchronizePDFGanjoorLinkAsync(linkId);
            if (!string.IsNullOrEmpty(suggestion.ExceptionString))
                return BadRequest(suggestion.ExceptionString);
            return Ok();
        }

        /// <summary>
        /// next un-ocred pdf book
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("ocr/nextunocred")]
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(PDFBook))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetNextUnOCRedPDFBookAsync()
        {
            RServiceResult<PDFBook> res = await _pdfService.GetNextUnOCRedPDFBookAsync();
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
           
            return Ok(res.Result);
        }

        /// <summary>
        /// set page ocr info
        /// </summary>
        /// <param name="pdf"></param>
        /// <returns></returns>

        [HttpPut]
        [Route("ocr")]
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> SetPDFPageOCRInfoAsync([FromBody] PDFPageOCRDataViewModel pdf)
        {

            RServiceResult<bool> res = await _pdfService.SetPDFPageOCRInfoAsync(pdf);
            if (!string.IsNullOrEmpty(res.ExceptionString))
            {
                return BadRequest(res.ExceptionString);
            }
            return Ok();
        }

        /// <summary>
        /// reset ocr queue
        /// </summary>
        /// <returns></returns>
        [HttpDelete("ocr/queue")]
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> ResetOCRQueueAsync()
        {
            var res = await _pdfService.ResetOCRQueueAsync();
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok();
        }

        /// <summary>
        /// fill book text
        /// </summary>
        /// <returns></returns>
        [HttpPut]
        [Route("ocr/fillbooktext")]
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public IActionResult StartFillingMissingBookTextsAsync()
        {

            _pdfService.StartFillingMissingBookTextsAsync();
            return Ok();
        }

        /// <summary>
        /// page of published book by page number
        /// </summary>
        /// <param name="pdfBookId"></param>
        /// <param name="pageNumber"></param>
        /// <returns></returns>
        [HttpGet("{pdfBookId}/page/{pageNumber}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(PDFPage))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetPDFPageAsync(int pdfBookId, int pageNumber)
        {
            var bookRes = await _pdfService.GetPDFPageAsync(pdfBookId, pageNumber);

            if (!string.IsNullOrEmpty(bookRes.ExceptionString))
            {
                return BadRequest(bookRes.ExceptionString);
            }
            if (bookRes.Result == null)
                return NotFound();

            Response.GetTypedHeaders().LastModified = bookRes.Result.LastModified;

            var requestHeaders = Request.GetTypedHeaders();
            if (requestHeaders.IfModifiedSince.HasValue &&
                requestHeaders.IfModifiedSince.Value >= bookRes.Result.LastModified)
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }


            return Ok(bookRes.Result);
        }

        /// <summary>
        /// queued downloding pdf books
        /// </summary>
        /// <param name="paging"></param>
        /// <returns></returns>
        [HttpGet("q")]
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<QueuedPDFBook>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]

        public async Task<IActionResult> GetQueuedPDFBooksAsync([FromQuery] PagingParameterModel paging)
        {
            var pdfBooksInfo = await _pdfService.GetQueuedPDFBooksAsync(paging);
            if (!string.IsNullOrEmpty(pdfBooksInfo.ExceptionString))
            {
                return BadRequest(pdfBooksInfo.ExceptionString);
            }


            // Paging Header
            HttpContext.Response.Headers.Add("paging-headers", JsonConvert.SerializeObject(pdfBooksInfo.Result.PagingMeta));

            return Ok(pdfBooksInfo.Result.Books);
        }

        /// <summary>
        /// delete queued books
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>

        [HttpDelete("q")]
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + SecurableItem.DeleteOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> DeleteQueuedPDFBookAsync(Guid id)
        {
            RServiceResult<bool> res = await _pdfService.DeleteQueuedPDFBookAsync(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
            {
                return BadRequest(res.ExceptionString);
            }
            return Ok(res.Result);
        }

        /// <summary>
        /// mix queued pdf books 
        /// </summary>
        /// <param name="step"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("q/mix/{step}")]
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> MixQueuedPDFBooksAsync(int step = 10)
        {

            var res = await _pdfService.MixQueuedPDFBooksAsync(step);
            if (!string.IsNullOrEmpty(res.ExceptionString))
            {
                return BadRequest(res.ExceptionString);
            }
            return Ok();
        }

        /// <summary>
        /// start processing queue pdf books
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("q/process/{count}")]
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public IActionResult StartProcessingQueuedPDFBooks(int count = 1000)
        {

            _pdfService.StartProcessingQueuedPDFBooks(count);
            return Ok();
        }


        /// <summary>
        /// PDF Service
        /// </summary>
        protected readonly IPDFLibraryService _pdfService;

        /// <summary>
        /// IUserPermissionChecker instance
        /// </summary>
        protected IUserPermissionChecker _userPermissionChecker;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="pdfService"></param>
        /// <param name="userPermissionChecker"></param>
        public PDFLibraryController(IPDFLibraryService pdfService, IUserPermissionChecker userPermissionChecker)
        {
            _pdfService = pdfService;
            _userPermissionChecker = userPermissionChecker;
        }
    }
}
