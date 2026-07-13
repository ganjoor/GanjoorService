using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace GanjooRazor.Utils
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
        protected async Task<IActionResult> WithSecureClientAsync(
            Func<HttpClient, Task<IActionResult>> operation,
            IActionResult unauthorizedResult = null)
        {
            using var secureClient = new HttpClient();
            if (!await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
            {
                return unauthorizedResult ?? new BadRequestObjectResult(NotLoggedInMessage);
            }
            return await operation(secureClient);
        }
    }
}
