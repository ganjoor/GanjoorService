using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor.ViewModels;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace GanjooRazor.Pages
{
    /// <summary>
    /// The site's home page only. Used to also serve every poem/poet/category page (dispatching
    /// internally on Request.Path == "/") - that responsibility moved to GanjoorPageModel, which now
    /// owns the site's catch-all route (see Startup.cs). This page keeps its own automatic "/" route,
    /// unaffected by that change.
    /// </summary>
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class IndexModel : LoginPartialEnabledPageModel
    {
        private readonly PoetCacheService _poetCache;

        /// <summary>
        /// constructor
        /// </summary>
        public IndexModel(IConfiguration configuration,
            HttpClient httpClient,
            PoetCacheService poetCache
            ) : base(httpClient, configuration)
        {
            _poetCache = poetCache;
        }

        public bool OfflineMode => GetConfigFlag("OfflineMode");

        public bool ReadOnlyMode => GetConfigFlag("ReadOnlyMode");

        /// <summary>
        /// last error
        /// </summary>
        public string LastError { get; set; }

        /// <summary>
        /// If the response failed, stores the API's error message in <see cref="LastError"/> and
        /// returns true so the caller can short-circuit.
        /// </summary>
        private async Task<bool> CaptureErrorIfFailedAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return false;
            }
            LastError = await ReadErrorMessageAsync(response);
            return true;
        }

        /// <summary>
        /// Poets (for the home page's own search-form author dropdown)
        /// </summary>
        public List<GanjoorPoetViewModel> Poets { get; set; }

        private async Task<bool> preparePoets()
        {
            var (success, poets, error) = await _poetCache.GetPoetsAsync(AggressiveCacheEnabled);
            if (!success)
            {
                LastError = error;
                return false;
            }
            Poets = poets;
            return true;
        }

        /// <summary>
        /// poets grouped by century, shown on the home page
        /// </summary>
        public List<GanjoorCenturyViewModel> PoetGroups { get; set; }

        private async Task<bool> _PreparePoetGroups()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/centuries");
                if (await CaptureErrorIfFailedAsync(response))
                {
                    return false;
                }
                PoetGroups = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorCenturyViewModel>>();
                return true;
            }
            catch
            {
                LastError = "خطا در دسترسی به وب سرویس گنجور";
                return false;
            }
        }

        /// <summary>
        /// Handles the legacy "?p=&lt;id&gt;" query-string form by resolving it to the page's real
        /// URL and redirecting there. Needed here too (not just on GanjoorPageModel): query strings
        /// don't affect ASP.NET Core route matching, so a link like "/?p=123" always has
        /// Path == "/" and is always routed to this page regardless of the split.
        /// </summary>
        private async Task<IActionResult> RedirectByPageIdAsync(string pageId)
        {
            var pageUrlResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/pageurl?id={pageId}");
            if (await CaptureErrorIfFailedAsync(pageUrlResponse))
            {
                return Page();
            }
            var pageUrl = JsonConvert.DeserializeObject<string>(await pageUrlResponse.Content.ReadAsStringAsync());
            return Redirect(pageUrl);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var maintenanceResult = TryGetMaintenanceModeResult();
            if (maintenanceResult != null)
            {
                return maintenanceResult;
            }

            LastError = "";
            InitializeCommonPageState();

            if (!string.IsNullOrEmpty(Request.Query["p"]))
            {
                return await RedirectByPageIdAsync(Request.Query["p"]);
            }

            if (!await preparePoets())
            {
                return Page();
            }

            if (!await _PreparePoetGroups())
            {
                return Page();
            }

            ViewData["Title"] = "گنجور";

            return Page();
        }

        public async Task<IActionResult> OnGetPoetInformationAsync(int id)
        {
            if (id == 0)
            {
                return new OkObjectResult(null);
            }
            var (success, poet, error) = await _poetCache.GetPoetAsync(id, AggressiveCacheEnabled);
            if (!success)
            {
                return BadRequest(error);
            }
            return new OkObjectResult(poet);
        }
    }
}
