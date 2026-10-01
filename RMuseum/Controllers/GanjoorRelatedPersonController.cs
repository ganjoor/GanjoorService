using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RMuseum.Models.Auth.Memory;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
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
    /// related people (family tree / person tagging) - reads are anonymous; the only write paths are
    /// the suggestion queue below (any logged-in user may suggest an edit to an existing person,
    /// same as suggesting a poem correction; a moderator reviews it the same way a poem correction is
    /// reviewed). There is no direct-edit endpoint for GanjoorRelatedPerson.
    /// </summary>
    [Produces("application/json")]
    [Route("api/people")]
    public class GanjoorRelatedPersonController : Controller
    {
        /// <summary>
        /// get all people
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorRelatedPerson[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetPeopleAsync()
        {
            var res = await _personService.GetPeopleAsync();
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get people who caption a family tree - the entry points for browsing family trees.
        /// Registered before the "{id}" route below (and constrained to int there) so this literal
        /// segment isn't swallowed as an id.
        /// </summary>
        /// <returns></returns>
        [HttpGet("familytrees")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorRelatedPerson[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetFamilyTreeRootsAsync()
        {
            var res = await _personService.GetFamilyTreeRootsAsync();
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get the whole known network of people (every person with at least one kinship edge or
        /// non-family tie, plus every one of those edges/ties), for the force-directed "ontology"
        /// explorer opened via the PeopleExplorer.open() modal (formerly the standalone page
        /// /PeopleGraph). Registered before the "{id}" route below (and constrained to
        /// int there) so this literal segment isn't swallowed as an id.
        /// </summary>
        /// <returns></returns>
        [HttpGet("graph")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPersonGraphViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetPersonGraphAsync()
        {
            var res = await _personService.GetPersonGraphAsync();
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get person by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorRelatedPerson))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetPersonAsync(int id)
        {
            var res = await _personService.GetPersonAsync(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get a person along with all their kinship/affiliation edges, for the read-only
        /// person/family-tree browsing page
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id:int}/relations")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPersonRelationsViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetPersonRelationsAsync(int id)
        {
            var res = await _personService.GetPersonRelationsAsync(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get the poems tagged with this person
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id:int}/poems")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(PoemGeoDateTag[]))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetPoemsByPersonAsync(int id)
        {
            var res = await _personService.GetPoemsByPersonAsync(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get the whole connected kinship component reachable from this person (ancestors,
        /// descendants, spouses, siblings), for the interactive family-tree chart
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id:int}/familytree")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorFamilyTreeViewModel))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetFamilyTreeAsync(int id)
        {
            var res = await _personService.GetFamilyTreeAsync(id);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// suggest an edit to an already-approved person's own fields - any logged-in user, same as
        /// suggesting a poem correction. Goes into the pending queue; does not change the person.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="suggestion"></param>
        /// <returns></returns>
        [HttpPost("{id:int}/editsuggestion")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPersonEditSuggestion))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> SuggestPersonEditAsync(int id, [FromBody] GanjoorPersonEditSuggestion suggestion)
        {
            suggestion.PersonId = id;
            suggestion.UserId = new Guid(User.Claims.First(c => c.Type == "UserId").Value);
            var res = await _personService.SuggestPersonEditAsync(suggestion);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get the next unreviewed person-edit suggestion, for the moderator queue - same permission
        /// as reviewing a poem correction
        /// </summary>
        /// <param name="skip"></param>
        /// <returns></returns>
        [HttpGet("editsuggestions/next")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPersonEditSuggestion))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetNextUnreviewedPersonEditSuggestionAsync(int skip = 0)
        {
            var res = await _personService.GetNextUnreviewedPersonEditSuggestionAsync(skip);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);

            var resCount = await _personService.GetUnreviewedPersonEditSuggestionCountAsync();
            if (!string.IsNullOrEmpty(resCount.ExceptionString))
                return BadRequest(resCount.ExceptionString);

            // Paging Header - same shape ReviewEdits.cshtml.cs already knows how to read for poem corrections
            HttpContext.Response.Headers.Append("paging-headers",
                JsonConvert.SerializeObject(
                    new PaginationMetadata()
                    {
                        totalCount = resCount.Result,
                        pageSize = -1,
                        currentPage = -1,
                        hasNextPage = false,
                        hasPreviousPage = false,
                        totalPages = -1
                    })
                );

            return Ok(res.Result);//might be null
        }

        /// <summary>
        /// apply a moderator's decision to a pending person-edit suggestion - same permission as
        /// moderating a poem correction
        /// </summary>
        /// <param name="id"></param>
        /// <param name="moderation"></param>
        /// <returns></returns>
        [HttpPost("editsuggestions/{id:int}/moderate")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPersonEditSuggestion))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> ModeratePersonEditSuggestionAsync(int id, [FromBody] PersonEditSuggestionModerationViewModel moderation)
        {
            Guid userId = new Guid(User.Claims.First(c => c.Type == "UserId").Value);
            var res = await _personService.ModeratePersonEditSuggestionAsync(userId, id, moderation.Result, moderation.ReviewNote);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get a single kinship edge by its own id, with both sides' names resolved - used by
        /// /SuggestPersonRelationEdit/{relationId} to show what it's about
        /// </summary>
        /// <param name="relationId"></param>
        /// <returns></returns>
        [HttpGet("relations/{relationId:int}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPersonRelation))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetRelationByIdAsync(int relationId)
        {
            var res = await _personService.GetRelationByIdAsync(relationId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get a single affiliation edge by its own id, with both sides' names resolved - used by
        /// /SuggestPersonRelationEdit?affiliationId={affiliationId} to show what it's about
        /// </summary>
        /// <param name="affiliationId"></param>
        /// <returns></returns>
        [HttpGet("affiliations/{affiliationId:int}")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPersonAffiliation))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetAffiliationByIdAsync(int affiliationId)
        {
            var res = await _personService.GetAffiliationByIdAsync(affiliationId);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// suggest an addition, change or removal of a kinship edge - any logged-in user, same as
        /// suggesting a person edit. Goes into the pending queue; does not change anything.
        /// </summary>
        /// <param name="suggestion"></param>
        /// <returns></returns>
        [HttpPost("relationeditsuggestion")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPersonRelationEditSuggestion))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> SuggestPersonRelationEditAsync([FromBody] GanjoorPersonRelationEditSuggestion suggestion)
        {
            suggestion.UserId = new Guid(User.Claims.First(c => c.Type == "UserId").Value);
            var res = await _personService.SuggestPersonRelationEditAsync(suggestion);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// get the next unreviewed relation-edit suggestion, for the moderator queue - same
        /// permission as reviewing a person edit
        /// </summary>
        /// <param name="skip"></param>
        /// <returns></returns>
        [HttpGet("relationeditsuggestions/next")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPersonRelationEditSuggestion))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> GetNextUnreviewedPersonRelationEditSuggestionAsync(int skip = 0)
        {
            var res = await _personService.GetNextUnreviewedPersonRelationEditSuggestionAsync(skip);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);

            var resCount = await _personService.GetUnreviewedPersonRelationEditSuggestionCountAsync();
            if (!string.IsNullOrEmpty(resCount.ExceptionString))
                return BadRequest(resCount.ExceptionString);

            // Paging Header - same shape ReviewEdits.cshtml.cs already knows how to read for poem corrections
            HttpContext.Response.Headers.Append("paging-headers",
                JsonConvert.SerializeObject(
                    new PaginationMetadata()
                    {
                        totalCount = resCount.Result,
                        pageSize = -1,
                        currentPage = -1,
                        hasNextPage = false,
                        hasPreviousPage = false,
                        totalPages = -1
                    })
                );

            return Ok(res.Result);//might be null
        }

        /// <summary>
        /// apply a moderator's decision to a pending relation-edit suggestion - same permission as
        /// moderating a person edit
        /// </summary>
        /// <param name="id"></param>
        /// <param name="moderation"></param>
        /// <returns></returns>
        [HttpPost("relationeditsuggestions/{id:int}/moderate")]
        [Authorize(Policy = RMuseumSecurableItem.GanjoorEntityShortName + ":" + SecurableItem.ModifyOperationShortName)]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GanjoorPersonRelationEditSuggestion))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        public async Task<IActionResult> ModeratePersonRelationEditSuggestionAsync(int id, [FromBody] PersonEditSuggestionModerationViewModel moderation)
        {
            Guid userId = new Guid(User.Claims.First(c => c.Type == "UserId").Value);
            var res = await _personService.ModeratePersonRelationEditSuggestionAsync(userId, id, moderation.Result, moderation.ReviewNote);
            if (!string.IsNullOrEmpty(res.ExceptionString))
                return BadRequest(res.ExceptionString);
            return Ok(res.Result);
        }

        /// <summary>
        /// person service
        /// </summary>
        private readonly IGanjoorRelatedPersonService _personService;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="personService"></param>
        public GanjoorRelatedPersonController(IGanjoorRelatedPersonService personService)
        {
            _personService = personService;
        }
    }
}
