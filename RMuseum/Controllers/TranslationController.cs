using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RMuseum.Models.Auth.Memory;
using RMuseum.Models.Ganjoor;
using RMuseum.Services;
using System;
using System.Net;
using System.Threading.Tasks;

namespace RMuseum.Controllers
{
    [Produces("application/json")]
    [Route("api/translations")]
    public class TranslationController : Controller
    {
        /// <summary>
        /// add new langauge
        /// </summary>
        /// <param name="lang"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.Translations)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorLanguage))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> AddLanguageAsync([FromBody] GanjoorLanguage lang)
        {
            var res = await _translationService.AddLanguageAsync(lang.Name, lang.RightToLeft);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// translation service
        /// </summary>
        private readonly IGanjoorTranslationService _translationService;

        public TranslationController(IGanjoorTranslationService translationService)
        {
            _translationService = translationService;
        }
    }
}
