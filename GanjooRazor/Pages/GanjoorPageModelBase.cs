using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using GanjooRazor.Utils;

namespace GanjooRazor.Pages
{
    /// <summary>
    /// Base class for Razor page models that call the Ganjoor API.
    ///
    /// Centralizes three patterns that were previously copy-pasted across dozens of page-model
    /// files in this project (Index.cshtml.cs alone had ~35+10+14 copies before this class existed):
    ///  1) reading the API's JSON-encoded error string out of a failed response
    ///  2) running a call against an HttpClient authenticated from the current session cookies
    ///  3) the shared "please log back in" message shown when that authentication fails
    ///
    /// <see cref="LoginPartialEnabledPageModel"/> (the base class used by public-site pages) derives
    /// from this. Admin/User-area page models that used to derive directly from <see cref="PageModel"/>
    /// can derive from this instead to get the same helpers without inheriting the public-site-specific
    /// properties (GanjoorPage, NextUrl, etc.) that live on <see cref="LoginPartialEnabledPageModel"/>.
    /// </summary>
    public class GanjoorPageModelBase : PageModel
    {
        /// <summary>
        /// Message shown whenever an action requiring a session couldn't prepare an authenticated
        /// client (expired/missing cookies).
        /// </summary>
        protected const string NotLoggedInMessage = "لطفاً از گنجور خارج و مجددا به آن وارد شوید.";

        /// <summary>
        /// HttpClient instance for unauthenticated/public calls (injected, shared/pooled by the DI
        /// container - see Program.cs/Startup.cs registration).
        /// </summary>
        protected readonly HttpClient _httpClient;

        protected GanjoorPageModelBase(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Reads the API's JSON-encoded error string out of a failed response body.
        /// </summary>
        protected static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response)
        {
            return JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
        }

        /// <summary>
        /// Shape of the special error string the API sends (still just a plain string, see
        /// GanjoorService._BuildSanitizerDroppedTextError on the RMuseum side) when a comment/note/
        /// suggestion was rejected because sanitizing it had to drop real text. Message is the
        /// human-readable explanation; RemainingText is the plain text that would have remained,
        /// so the client can diff it against what the user actually typed and show exactly what
        /// would have been dropped.
        /// </summary>
        protected class SanitizerTextDroppedInfo
        {
            public bool SanitizerTextDropped { get; set; }
            public string Message { get; set; }
            public string RemainingText { get; set; }
        }

        /// <summary>
        /// Returns the parsed <see cref="SanitizerTextDroppedInfo"/> if rawErrorMessage is that
        /// special shape, or null for any ordinary plain-text error (including when rawErrorMessage
        /// isn't JSON at all, which is the common case).
        /// </summary>
        protected static SanitizerTextDroppedInfo TryParseSanitizerTextDroppedError(string rawErrorMessage)
        {
            if (string.IsNullOrWhiteSpace(rawErrorMessage) || rawErrorMessage.TrimStart()[0] != '{')
                return null;
            try
            {
                var info = JsonConvert.DeserializeObject<SanitizerTextDroppedInfo>(rawErrorMessage);
                return (info != null && info.SanitizerTextDropped) ? info : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Runs <paramref name="operation"/> against an HttpClient authenticated from the current
        /// session cookies (via <see cref="GanjoorSessionChecker.PrepareClient"/>). If the session
        /// can't be prepared (missing/expired cookies), returns <paramref name="unauthorizedResult"/>
        /// (defaulting to a 400 with <see cref="NotLoggedInMessage"/>) instead of every handler
        /// re-implementing the same using/if/else block.
        ///
        /// Intended for AJAX-style handlers that return a JSON/partial result. Full-page POST
        /// handlers that need to re-render the page with an inline error message on auth failure
        /// (rather than a bare 400) should keep their own using/PrepareClient block instead - wrapping
        /// those here would silently change what the browser shows on a real (non-AJAX) form submit.
        /// </summary>
        /// <summary>
        /// Builds the BadRequest to return for an AJAX handler from a failed API response: the
        /// plain error string as before for an ordinary error, or - when it's the "sanitizing had
        /// to drop real text" case - a small JSON object ({ sanitizerTextDropped, message,
        /// remainingText }) so the client's error callback can show the user what got dropped
        /// instead of just displaying raw text.
        /// </summary>
        protected static async Task<IActionResult> BadRequestFromApiErrorAsync(HttpResponseMessage response)
        {
            string rawError = await ReadErrorMessageAsync(response);
            var sanitizerInfo = TryParseSanitizerTextDroppedError(rawError);
            if (sanitizerInfo != null)
            {
                return new BadRequestObjectResult(new
                {
                    sanitizerTextDropped = true,
                    message = sanitizerInfo.Message,
                    remainingText = sanitizerInfo.RemainingText
                });
            }
            return new BadRequestObjectResult(rawError);
        }

        protected async Task<IActionResult> WithSecureClientAsync(
            Func<HttpClient, Task<IActionResult>> operation,
            IActionResult unauthorizedResult = null)
        {
            using var secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response));
            if (!await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
            {
                return unauthorizedResult ?? new BadRequestObjectResult(NotLoggedInMessage);
            }
            return await operation(secureClient);
        }
    }
}
