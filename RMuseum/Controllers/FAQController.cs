using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RMuseum.Models.Auth.Memory;
using RMuseum.Services;
using System.Net;
using System.Threading.Tasks;
using RMuseum.Models.FAQ;

namespace RMuseum.Controllers
{
    [Produces("application/json")]
    [Route("api/faq")]
    public class FAQController : Controller
    {
        /// <summary>
        /// get published categories
        /// </summary>
        /// <returns></returns>
        [HttpGet("cat")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(FAQCategory[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetCategoriesAsync()
        {
            var res = await _faqService.GetCategoriesAsync(true);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }
        /// <summary>
        /// get all categories
        /// </summary>
        /// <returns></returns>

        [HttpGet("cat/secure")]
        [Authorize(Policy = RMuseumSecurableItem.FAQEntityShortName + ":" + RMuseumSecurableItem.ModerateOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(FAQCategory[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetAllCategoriesAsync()
        {
            var res = await _faqService.GetCategoriesAsync(false);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get category by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("cat/secure/{id}")]
        [Authorize(Policy = RMuseumSecurableItem.FAQEntityShortName + ":" + RMuseumSecurableItem.ModerateOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(FAQCategory))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetCategoryByIdAsync(int id)
        {
            var res = await _faqService.GetCategoryByIdAsync(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get published pinned items
        /// </summary>
        /// <returns></returns>
        [HttpGet("pinned")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(FAQItem[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetPinnedItemsAsync()
        {
            var res = await _faqService.GetPinnedItemsAsync(true);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get published category items
        /// </summary>
        /// <param name="catId"></param>
        /// <returns></returns>

        [HttpGet("cat/items")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(FAQItem[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetCategoryItemsAsync(int catId)
        {
            var res = await _faqService.GetCategoryItemsAsync(catId, true);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get all category items
        /// </summary>
        /// <returns></returns>

        [HttpGet("cat/items/secure")]
        [Authorize(Policy = RMuseumSecurableItem.FAQEntityShortName + ":" + RMuseumSecurableItem.ModerateOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(FAQItem[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetAllCategoryItemsAsync(int catId)
        {
            var res = await _faqService.GetCategoryItemsAsync(catId, false);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get item by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("secure/{id}")]
        [Authorize(Policy = RMuseumSecurableItem.FAQEntityShortName + ":" + RMuseumSecurableItem.ModerateOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(FAQItem))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetItemByIdAsync(int id)
        {
            var res = await _faqService.GetItemByIdAsync(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }


        /// <summary>
        /// FAQ Service
        /// </summary>
        protected readonly IFAQService _faqService;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="faqService"></param>
        public FAQController(IFAQService faqService)
        {
            _faqService = faqService;
        }
    }
}
