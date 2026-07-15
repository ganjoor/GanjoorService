using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor.ViewModels;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace GanjooRazor.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class QuotesModel : LoginPartialEnabledPageModel
    {
        private readonly PoetCacheService _poetCache;

        public QuotesModel(HttpClient httpClient, IConfiguration configuration, PoetCacheService poetCache) : base(httpClient, configuration)
        {
            _poetCache = poetCache;
        }

        public string LastError { get; set; }
        public List<GanjoorPoetViewModel> Poets { get; set; }
        public GanjoorPoetViewModel Poet { get; set; }
        public List<GanjoorQuotedPoemViewModel> ClaimedQuotes { get; set; }
        public List<GanjoorQuotedPoemViewModel> Quotes { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var maintenanceResult = TryGetMaintenanceModeResult();
            if (maintenanceResult != null)
            {
                return maintenanceResult;
            }

            InitializeCommonPageState();

            var (poetsOk, poets, poetsError) = await _poetCache.GetPoetsAsync(AggressiveCacheEnabled);
            if (!poetsOk)
            {
                LastError = poetsError;
                return Page();
            }
            Poets = poets;

            if (!string.IsNullOrEmpty(Request.Query["p"]))
            {
                // Fetched by URL slug rather than id, and returns the "complete" poet shape rather
                // than the list shape - different enough from PoetCacheService's GetPoetAsync(id)
                // that it stays a direct call here rather than being folded into that service.
                var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poet?url=/{Request.Query["p"]}");
                if (!response.IsSuccessStatusCode)
                {
                    LastError = await ReadErrorMessageAsync(response);
                    return Page();
                }
                Poet = JObject.Parse(await response.Content.ReadAsStringAsync()).ToObject<GanjoorPoetCompleteViewModel>().Poet;
                Poet.ImageUrl = $"{APIRoot.InternetUrl}{Poet.ImageUrl}";
            }

            var url = $"{APIRoot.Url}/api/ganjoor/quoted?published=true";
            if (Poet != null)
            {
                url += $"&poetId={Poet.Id}";
            }
            var responseQuotes = await _httpClient.GetAsync(url + "&claimed=false");
            if (!responseQuotes.IsSuccessStatusCode)
            {
                LastError = await ReadErrorMessageAsync(responseQuotes);
                return Page();
            }
            Quotes = JArray.Parse(await responseQuotes.Content.ReadAsStringAsync()).ToObject<List<GanjoorQuotedPoemViewModel>>();

            var responseClaimedQuotes = await _httpClient.GetAsync(url + "&claimed=true");
            if (!responseClaimedQuotes.IsSuccessStatusCode)
            {
                LastError = await ReadErrorMessageAsync(responseClaimedQuotes);
                return Page();
            }
            ClaimedQuotes = JArray.Parse(await responseClaimedQuotes.Content.ReadAsStringAsync()).ToObject<List<GanjoorQuotedPoemViewModel>>();

            ViewData["Title"] = Poet == null ? "نقل قول‌های شاعران" : $"نقل قول‌ها و شعرهای مرتبط {Poet.Nickname}";

            return Page();
        }
    }
}
