using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RMuseum.Models.Auth.Memory;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Services;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace RMuseum.Controllers
{
    [Produces("application/json")]
    [Route("api/translations")]
    public class TranslationController : Controller
    {
        /// <summary>
        /// get all languages
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorLanguage[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetLanguagesAsync()
        {
            var res = await _translationService.GetLanguagesAsync();
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get langauge by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>

        [HttpGet("{id}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorLanguage))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetLanguageAsync(int id)
        {
            var res = await _translationService.GetLanguageAsync(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

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
            var res = await _translationService.AddLanguageAsync(lang);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// update an existing language
        /// </summary>
        /// <param name="lang"></param>
        /// <returns></returns>
        [HttpPut]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.Translations)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> UpdateLangaugeAsync([FromBody] GanjoorLanguage lang)
        {
            var res = await _translationService.UpdateLangaugeAsync(lang);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.Translations)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> DeleteLangaugeAsync(int id)
        {
            var res = await _translationService.DeleteLangaugeAsync(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get poem translations
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("poem/{id}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPoemTranslationViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetTranslationsAsync(int id)
        {
            var res = await _translationService.GetTranslationsAsync(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get poem translation in a specific language
        /// </summary>
        /// <param name="id"></param>
        /// <param name="lang"></param>
        /// <returns></returns>
        [HttpGet("poem/{id}/langauge/{lang}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPoemTranslationViewModel))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetTranslationAsync(int id, int lang)
        {
            var res = await _translationService.GetTranslationAsync(id, lang);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            if (res.Result == null)
                return NotFound();
            return Ok(res.Result);
        }

        /// <summary>
        /// add translation for a poem
        /// </summary>
        /// <param name="translation"></param>
        /// <returns></returns>

        [HttpPost("poem")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.Translations)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> AddOrUpdatePoemTranslation([FromBody] GanjoorPoemTranslationViewModel translation)
        {
            Guid userId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            var res = await _translationService.AddPoemTranslation(userId, translation);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// translation service
        /// </summary>
        private readonly IGanjoorTranslationService _translationService;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="translationService"></param>
        public TranslationController(IGanjoorTranslationService translationService)
        {
            _translationService = translationService;
        }
    }
}
