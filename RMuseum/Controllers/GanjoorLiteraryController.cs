using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RMuseum.Models.Auth.Memory;
using RMuseum.Models.Ganjoor;
using RMuseum.Services;
using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Models.Generic;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace RMuseum.Controllers
{
    /// <summary>
    /// literary devices (آرایه‌های ادبی), concepts and poem literary tags
    /// </summary>
    [Produces("application/json")]
    [Route("api/ganjoor/literary")]
    public class GanjoorLiteraryController : Controller
    {
        /// <summary>
        /// approved devices with their slots
        /// </summary>
        [HttpGet]
        [Route("devices")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(LiteraryDevice[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetDevices()
        {
            var res = await _ganjoorService.GetLiteraryDevicesAsync(LiteraryEntityStatus.Approved);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// the not yet reviewed devices suggested by the current user (selectable by that user in the editor)
        /// </summary>
        [HttpGet]
        [Route("devices/mine")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(LiteraryDevice[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetMyPendingDevices()
        {
            Guid userId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            var res = await _ganjoorService.GetMyPendingLiteraryDevicesAsync(userId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// suggested (not yet reviewed) devices
        /// </summary>
        [HttpGet]
        [Route("devices/suggested")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(LiteraryDevice[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetSuggestedDevices()
        {
            var res = await _ganjoorService.GetLiteraryDevicesAsync(LiteraryEntityStatus.Suggested);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// suggest a new device
        /// </summary>
        [HttpPost]
        [Route("device")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(LiteraryDevice))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> SuggestDevice([FromBody] LiteraryDevice device)
        {
            Guid userId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            var res = await _ganjoorService.SuggestLiteraryDeviceAsync(userId, device);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// approve (with edits) or reject a suggested device: Status of the posted device is the decision
        /// </summary>
        [HttpPost]
        [Route("device/moderate")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(LiteraryDevice))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> ModerateDevice([FromBody] LiteraryDevice device)
        {
            Guid userId = new Guid(User.Claims.FirstOrDefault(c => c.Type == "UserId").Value);
            var res = await _ganjoorService.ModerateLiteraryDeviceAsync(userId, device);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// add the core devices of the initial list (those missing)
        /// </summary>
        [HttpPost]
        [Route("devices/seed-core")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(int))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> SeedCoreDevices()
        {
            var res = await _ganjoorService.SeedCoreLiteraryDevicesAsync();
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// import literary tags from an annotated corpus file (dryRun=true only reports what would happen)
        /// </summary>
        [HttpPost]
        [Route("import/corpus")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [RequestSizeLimit(100_000_000)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(LiteraryCorpusImportResult))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> ImportCorpus([FromBody] LiteraryCorpusImportFile file, string urlPrefix = "/hafez/ghazal/", bool dryRun = true)
        {
            var res = await _ganjoorService.ImportLiteraryCorpusAsync(file, urlPrefix, dryRun);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// search approved concepts
        /// </summary>
        [HttpGet]
        [Route("concepts")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(LiteraryConcept[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> SearchConcepts(string query = "", int take = 20)
        {
            var res = await _ganjoorService.SearchLiteraryConceptsAsync(query, take);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// concepts by ids (comma separated)
        /// </summary>
        [HttpGet]
        [Route("concepts/by-ids")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(LiteraryConcept[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetConceptsByIds(string ids = "")
        {
            var list = (ids ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => int.TryParse(x, out int v) ? v : 0).Where(v => v > 0).ToArray();
            var res = await _ganjoorService.GetLiteraryConceptsByIdsAsync(list);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// edit a concept (name, aliases, parent)
        /// </summary>
        [HttpPut]
        [Route("concept")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(LiteraryConcept))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> UpdateConcept([FromBody] LiteraryConcept concept)
        {
            var res = await _ganjoorService.UpdateLiteraryConceptAsync(concept);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// merge a concept into another
        /// </summary>
        [HttpPost]
        [Route("concept/merge/{sourceId}/{targetId}")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> MergeConcepts(int sourceId, int targetId)
        {
            var res = await _ganjoorService.MergeLiteraryConceptsAsync(sourceId, targetId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok();
        }

        /// <summary>
        /// approved literary tags of a poem
        /// </summary>
        [HttpGet]
        [Route("poem/{id}/tags")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(PoemLiteraryTag[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetPoemTags(int id)
        {
            var res = await _ganjoorService.GetPoemLiteraryTagsAsync(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// whether the poem's literary annotation is marked complete
        /// </summary>
        [HttpGet]
        [Route("poem/{id}/complete")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetPoemAnnotationCompleted(int id)
        {
            var res = await _ganjoorService.GetPoemLiteraryAnnotationCompletedAsync(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// delete a literary tag directly (moderators; ordinary users suggest a deletion in the poem editor)
        /// </summary>
        [HttpDelete]
        [Route("tag/{id}")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> DeleteTag(int id)
        {
            var res = await _ganjoorService.DeletePoemLiteraryTagAsync(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok();
        }

        /// <summary>
        /// Ganjoor Service
        /// </summary>
        protected readonly IGanjoorService _ganjoorService;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="ganjoorService"></param>
        public GanjoorLiteraryController(IGanjoorService ganjoorService)
        {
            _ganjoorService = ganjoorService;
        }
    }
}
