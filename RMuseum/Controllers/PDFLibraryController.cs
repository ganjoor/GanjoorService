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
using RMuseum.Services.Implementation;

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
        /// <returns></returns>
        [HttpGet("secure/{id}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(PDFBook))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetUserVisiblePDFBookAsync(int id)
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
                bookRes = await _pdfService.GetPDFBookByIdAsync(id, new PublishStatus[] { PublishStatus.Published });
                if (!string.IsNullOrEmpty(bookRes.ExceptionString))
                {
                    return BadRequest(bookRes.ExceptionString);
                }
                if (bookRes.Result == null)
                    return NotFound();
            }
            if (bookRes == null)
            {
                bookRes = await _pdfService.GetPDFBookByIdAsync(id, visibleItems);
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
        /// <returns></returns>
        [HttpGet("{id}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(PDFBook))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetPDFBookByIdAsync(int id)
        {
            var bookRes = await _pdfService.GetPDFBookByIdAsync(id, new PublishStatus[] { PublishStatus.Published });

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
        /// add new tag value to artifact
        /// </summary>
        /// <param name="pdfBookId"></param>
        /// <param name="tag">only name is processed</param>
        /// <returns></returns>
        [HttpPost("tagvalue/{pdfBookId}")]
        [Authorize(Policy = RMuseumSecurableItem.ArtifactEntityShortName + ":" + RMuseumSecurableItem.EditTagValueOperationShortName)]
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
        [Authorize(Policy = RMuseumSecurableItem.ArtifactEntityShortName + ":" + RMuseumSecurableItem.EditTagValueOperationShortName)]
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
        [Authorize(Policy = RMuseumSecurableItem.ArtifactEntityShortName + ":" + RMuseumSecurableItem.EditTagValueOperationShortName)]
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
        public async Task<IActionResult> GetByTagValue(string tagUrl, string valueUrl)
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
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + SecurableItem.AddOperationShortName)]
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
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + SecurableItem.AddOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> DeleteAuthorAsync(int id)
        {
            var res = await _pdfService.DeleteAuthorAsync(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok();
        }

        /// <summary>
        /// add a new mukti volume pdf collection
        /// </summary>
        /// <param name="volumes"></param>
        /// <returns></returns>
        [HttpPost("volumes")]
        [Authorize(Policy = RMuseumSecurableItem.PDFLibraryEntityShortName + ":" + SecurableItem.AddOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(MultiVolumePDFCollection))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> AddMultiVolumePDFCollection([FromBody] MultiVolumePDFCollection volumes)
        {
            var res = await _pdfService.AddMultiVolumePDFCollection(volumes);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
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
