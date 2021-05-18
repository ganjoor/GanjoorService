using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RMuseum.Models.Auth.Memory;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Services;
using RSecurityBackend.Models.Generic;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace RMuseum.Controllers
{
    [Produces("application/json")]
    [Route("api/banners")]
    public class SiteBannersController : Controller
    {
        /// <summary>
        /// add site banner (send form) with these fields: alt, url and an image attachment
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.Banners)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorSiteBannerViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> AddSiteBanner()
        {
            try
            {
                if (!Request.Form.TryGetValue("alt", out Microsoft.Extensions.Primitives.StringValues alt))
                {
                    return BadRequest("alt is null");
                }
                if (!Request.Form.TryGetValue("url", out Microsoft.Extensions.Primitives.StringValues url))
                {
                    return BadRequest("url is null");
                }
                if (Request.Form.Files.Count != 1)
                {
                    return BadRequest("a single image is not provided");
                }
                using Stream stream = Request.Form.Files[0].OpenReadStream();
                RServiceResult<GanjoorSiteBannerViewModel> res = await _bannersService.AddSiteBanner(stream, Request.Form.Files[0].FileName, alt.ToString(), url.ToString(), false);
                if (!string.IsNullOrEmpty(res.ExceptionString))
                {
                    return BadRequest(res.ExceptionString);
                }
                return Ok(res.Result);
            }
            catch (Exception exp)
            {
                return BadRequest(exp.ToString());
            }
        }

        /// <summary>
        /// modify site banner
        /// </summary>
        /// <param name="id"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("{id}")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.Banners)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> ModifySiteBanner(int id, [FromBody] GanjoorSiteBannerModifyViewModel model)
        {
            try
            {

                RServiceResult<bool> res = await _bannersService.ModifySiteBanner(id, model.AlternateText, model.TargetUrl, model.Active);

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
            catch (Exception exp)
            {
                return BadRequest(exp.ToString());
            }
        }

        /// <summary>
        /// delete site banner
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.Banners)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteSiteBanner(int id)
        {
            try
            {

                RServiceResult<bool> res = await _bannersService.DeleteSiteBanner(id);

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
            catch (Exception exp)
            {
                return BadRequest(exp.ToString());
            }
        }

        /// <summary>
        /// get all defined site banners
        /// </summary>
        /// <returns></returns>

        [HttpGet]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.Banners)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorSiteBannerViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetSiteBanners()
        {
            try
            {
                RServiceResult<GanjoorSiteBannerViewModel[]> res = await _bannersService.GetSiteBanners();

                if (!string.IsNullOrEmpty(res.ExceptionString))
                {
                    return BadRequest(res.ExceptionString);
                }
                return Ok(res.Result);
            }
            catch (Exception exp)
            {
                return BadRequest(exp.ToString());
            }
        }

        /// <summary>
        /// get a random active site banner
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("random")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorSiteBannerViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetARandomActiveSiteBanner()
        {
            try
            {
                RServiceResult<GanjoorSiteBannerViewModel> res = await _bannersService.GetARandomActiveSiteBanner();

                if (!string.IsNullOrEmpty(res.ExceptionString))
                {
                    return BadRequest(res.ExceptionString);
                }

                return Ok(res.Result); //this might be null
            }
            catch (Exception exp)
            {
                return BadRequest(exp.ToString());
            }
        }

        /// <summary>
        /// Ganjoor Service
        /// </summary>

        protected readonly ISiteBannersService _bannersService;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="bannersService"></param>
        public SiteBannersController(ISiteBannersService bannersService)
        {
            _bannersService = bannersService;
        }
    }
}
