using Microsoft.AspNetCore.Mvc;
using RMuseum.Models.Ganjoor.SemanticSearch;
using RMuseum.Services.Implementation;
using RMuseum.Utils.SemanticSearch;
using System;
using System.Net;
using System.Threading.Tasks;

namespace RMuseum.Controllers
{
    /// <summary>
    /// Semantic ("find a poem about...") search — deliberately its own controller, not a method
    /// on GanjoorController, after a production incident: GanjoorController's constructor took
    /// ISemanticSearchService (indirectly requiring EmbeddingIndex/QueryEmbedder to load
    /// successfully), so a resource-loading failure prevented the ENTIRE controller from being
    /// constructed — a 503 on every endpoint under /api/ganjoor, not just this feature. Same
    /// route prefix as before (api/ganjoor), so the endpoint's URL is unchanged
    /// (POST /api/ganjoor/search/semantic) — only which controller class hosts it changed.
    /// A future failure in this feature's own dependencies can now only ever affect this one
    /// controller/endpoint, never GanjoorController or anything else.
    /// </summary>
    [Produces("application/json")]
    [Route("api/ganjoor")]
    [ApiController]
    public class SemanticSearchController : ControllerBase
    {
        protected readonly ISemanticSearchService _semanticSearchService;

        public SemanticSearchController(ISemanticSearchService semanticSearchService)
        {
            _semanticSearchService = semanticSearchService;
        }

        /// <summary>
        /// semantic ("find a poem about...") search
        /// </summary>
        [HttpPost("search/semantic")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.ServiceUnavailable, Type = typeof(string))]
        public async Task<IActionResult> SemanticSearch([FromBody] SemanticSearchRequestDto request)
        {
            try
            {
                var result = await _semanticSearchService.SearchAsync(request);
                return Ok(result);
            }
            catch (SemanticSearchUnavailableException exp)
            {
                // distinct from a plain 400/500 - lets a client (or a person reading logs) tell
                // "this feature isn't loaded/configured right now" apart from a bad query or an
                // actual crash
                return StatusCode((int)HttpStatusCode.ServiceUnavailable, exp.Message);
            }
            catch (ArgumentException exp)
            {
                return BadRequest(exp.Message);
            }
            catch (Exception exp)
            {
                return BadRequest(exp.ToString());
            }
        }

        /// <summary>
        /// Fire-and-forget click reporting — called via navigator.sendBeacon (or a keepalive
        /// fetch as fallback) right as a result link is clicked, so it can complete even as the
        /// browser navigates away. ReportClickAsync itself is fully best-effort (never throws in
        /// a way that matters here), so this always returns 200 regardless of whether the
        /// underlying write actually succeeded — the caller isn't listening for the response
        /// either way.
        /// </summary>
        [HttpPost("search/semantic/click")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> SemanticSearchClick([FromBody] SemanticSearchClickDto click)
        {
            await _semanticSearchService.ReportClickAsync(click);
            return Ok();
        }
    }
}
