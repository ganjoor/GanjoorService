using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RMuseum.Models.Auth.Memory;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Services;
using RSecurityBackend.Models.Auth.Memory;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace RMuseum.Controllers
{
    [Produces("application/json")]
    [Route("api/numberings")]
    public class NumberingController : Controller
    {
        /// <summary>
        /// get all numberings
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorNumbering[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetNumberingsAsync()
        {
            var res = await _numberingService.GetNumberingsAsync();
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get numbering by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>

        [HttpGet("{id}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorNumbering))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetNumberingAsync(int id)
        {
            var res = await _numberingService.GetNumberingAsync(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get all numberings related to a category
        /// </summary>
        /// <param name="id">category id</param>
        /// <returns></returns>
        [HttpGet("cat/{id}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorNumbering[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetNumberingsRelatedToCategoryAsync(int id)
        {
            var res1 = await _numberingService.GetNumberingsForCatAsync(id);
            if (!string.IsNullOrEmpty(res1.ExceptionString))
                return BadRequest(res1.ExceptionString);
            var res2 = await _numberingService.GetNumberingsForDirectSubCatsAsync(id);
            if (!string.IsNullOrEmpty(res2.ExceptionString))
                return BadRequest(res2.ExceptionString);
            List<GanjoorNumbering> combined = new List<GanjoorNumbering>();
            foreach (var numbering in res1.Result)
            {
                combined.Add(numbering);
            }
            foreach (var numbering in res2.Result)
            {
                if(combined.Where(n => n.Id == numbering.Id).FirstOrDefault() == null)
                {
                    combined.Add(numbering);
                }
            }
            return Ok(combined);
        }

        /// <summary>
        /// get all numbering patterns for a couplet
        /// </summary>
        /// <param name="poemId"></param>
        /// <param name="coupletIndex"></param>
        /// <returns></returns>
        [HttpGet("couplet/{poemId}/{coupletIndex}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorCoupletNumberViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetNumberingsForCouplet(int poemId, int coupletIndex)
        {
            var res = await _numberingService.GetNumberingsForCouplet(poemId, coupletIndex);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// add new numbering
        /// </summary>
        /// <param name="numbering"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorNumbering))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> AddNumberingAsync([FromBody] GanjoorNumbering numbering)
        {
            if (ReadOnlyMode)
                return BadRequest("سایت به دلایل فنی مثل انتقال سرور موقتاً در حالت فقط خواندنی قرار دارد. لطفاً ساعاتی دیگر مجدداً تلاش کنید.");
            var res = await _numberingService.AddNumberingAsync(numbering);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// update an existing numbering
        /// </summary>
        /// <param name="numbering"></param>
        /// <returns></returns>
        [HttpPut]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> UpdateNumberingAsync([FromBody] GanjoorNumbering numbering)
        {
            if (ReadOnlyMode)
                return BadRequest("سایت به دلایل فنی مثل انتقال سرور موقتاً در حالت فقط خواندنی قرار دارد. لطفاً ساعاتی دیگر مجدداً تلاش کنید.");
            var res = await _numberingService.UpdateNumberingAsync(numbering);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> DeleteNumberingAsync(int id)
        {
            if (ReadOnlyMode)
                return BadRequest("سایت به دلایل فنی مثل انتقال سرور موقتاً در حالت فقط خواندنی قرار دارد. لطفاً ساعاتی دیگر مجدداً تلاش کنید.");
            var res = await _numberingService.DeleteNumberingAsync(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// recount a numbering
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPut("recount/start/{id}")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public  IActionResult Recount(int id)
        {
            if (ReadOnlyMode)
                return BadRequest("سایت به دلایل فنی مثل انتقال سرور موقتاً در حالت فقط خواندنی قرار دارد. لطفاً ساعاتی دیگر مجدداً تلاش کنید.");
            var res = _numberingService.Recount(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// generate missing default numberings and start counting
        /// </summary>
        /// <returns></returns>
        [HttpPost("generatemissing")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public IActionResult GenerateMissingDefaultNumberings()
        {
            if (ReadOnlyMode)
                return BadRequest("سایت به دلایل فنی مثل انتقال سرور موقتاً در حالت فقط خواندنی قرار دارد. لطفاً ساعاتی دیگر مجدداً تلاش کنید.");
            var res = _numberingService.GenerateMissingDefaultNumberings();
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
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
        /// Configuration
        /// </summary>
        protected IConfiguration Configuration { get; }

        /// <summary>
        /// numbering service
        /// </summary>
        private readonly IGanjoorNumberingService _numberingService;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="numberingService"></param>
        /// <param name="configuration"></param>
        public NumberingController(IGanjoorNumberingService numberingService, IConfiguration configuration)
        {
            _numberingService = numberingService;
            Configuration = configuration;
        }
    }
}
