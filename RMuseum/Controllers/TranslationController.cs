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
        [HttpGet("languages")]
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
        /// get language by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>

        [HttpGet("languages/{id}")]
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
        /// add new language
        /// </summary>
        /// <param name="lang"></param>
        /// <returns></returns>
        [HttpPost("languages")]
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
        [HttpPut("languages")]
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

        [HttpDelete("languages/{id}")]
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
        /// get published poem translations
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("poem/{id}/published")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPoemTranslationViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetPublishedTranslationsAsync(int id)
        {
            var res = await _translationService.GetPoemTranslationsAsync(-1, id, true, false);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get published poem translation in a specific language
        /// </summary>
        /// <param name="id"></param>
        /// <param name="lang"></param>
        /// <returns></returns>
        [HttpGet("poem/{id}/language/{lang}/published")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPoemTranslationViewModel))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetPublishedTranslationAsync(int id, int lang)
        {
            var res = await _translationService.GetPoemTranslationsAsync(lang, id, true, false);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get all translations of a poem in a specific language
        /// </summary>
        /// <param name="id"></param>
        /// <param name="lang">send -1 for all languages</param>
        /// <returns></returns>
        [HttpGet("poem/{id}/language/{lang}")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.Translations)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPoemTranslationViewModel[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetPoemTranslationsAsync(int id, int lang)
        {
            var res = await _translationService.GetPoemTranslationsAsync(lang, id, false, true);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// last language user has contributed in its translation
        /// </summary>
        /// <returns></returns>

        [HttpGet("user/lastlanguage")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + RMuseumSecurableItem.Translations)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorLanguage))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetLastUserContributedLanguage()
        {
            Guid userId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            var res = await _translationService.GetLastUserContributedLanguage(userId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            if (res.Result == null)
                return NotFound();
            return Ok(res.Result);
        }

        /// <summary>
        /// add translation for a poem (please specify verses VORder, Id is not processed)
        /// </summary>
        /// <param name="translation"></param>
        /// <returns></returns>

        [HttpPost]
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
