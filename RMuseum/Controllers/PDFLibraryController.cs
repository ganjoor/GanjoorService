using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RMuseum.Models.Artifact;
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

namespace RMuseum.Controllers
{
    [Produces("application/json")]
    [Route("api/pdf")]
    public class PDFLibraryController : Controller
    {
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
        /// secure get a pdf book
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("secure/{id}")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(PDFBook))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetUserVisible(int id)
        {
            RServiceResult<PublishStatus[]> v = await _GetUserVisibleArtifactStatusSet
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

        private async Task<RServiceResult<PublishStatus[]>> _GetUserVisibleArtifactStatusSet(Guid loggedOnUserId, Guid sessionId)
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
