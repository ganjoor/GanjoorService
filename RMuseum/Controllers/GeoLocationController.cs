using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RMuseum.Models.Auth.Memory;
using RMuseum.Models.Ganjoor;
using RMuseum.Services;
using RSecurityBackend.Models.Auth.Memory;
using System.Net;
using System.Threading.Tasks;

namespace RMuseum.Controllers
{
    [Produces("application/json")]
    [Route("api/locations")]
    public class GeoLocationController : Controller
    {
        /// <summary>
        /// get all locations
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorGeoLocation[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetLocationsAsync()
        {
            var res = await _locationService.GetLocationsAsync();
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get location by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>

        [HttpGet("{id}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorGeoLocation))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetLocationAsync(int id)
        {
            var res = await _locationService.GetLocationAsync(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        
        /// <summary>
        /// add new location
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorGeoLocation))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> AddLocationAsync([FromBody] GanjoorGeoLocation location)
        {
            if (ReadOnlyMode)
                return BadRequest("سایت به دلایل فنی مثل انتقال سرور موقتاً در حالت فقط خواندنی قرار دارد. لطفاً ساعاتی دیگر مجدداً تلاش کنید.");
            var res = await _locationService.AddLocationAsync(location.Name, location.Latitude, location.Longitude);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// update an existing location
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        [HttpPut]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> UpdateLocationAsync([FromBody] GanjoorGeoLocation location)
        {
            if (ReadOnlyMode)
                return BadRequest("سایت به دلایل فنی مثل انتقال سرور موقتاً در حالت فقط خواندنی قرار دارد. لطفاً ساعاتی دیگر مجدداً تلاش کنید.");
            var res = await _locationService.UpdateLocationAsync(location);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> DeleteLocationAsync(int id)
        {
            if (ReadOnlyMode)
                return BadRequest("سایت به دلایل فنی مثل انتقال سرور موقتاً در حالت فقط خواندنی قرار دارد. لطفاً ساعاتی دیگر مجدداً تلاش کنید.");
            var res = await _locationService.DeleteLocationAsync(id);
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
        /// location service
        /// </summary>
        private readonly IGeoLocationService _locationService;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="locationService"></param>
        /// <param name="configuration"></param>
        public GeoLocationController(IGeoLocationService locationService, IConfiguration configuration)
        {
            _locationService = locationService;
            Configuration = configuration;
        }
    }
}
