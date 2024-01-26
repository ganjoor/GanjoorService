using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor.ViewModels;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace GanjooRazor.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class QuotesModel : LoginPartialEnabledPageModel
    {
        private readonly IConfiguration Configuration;
        public QuotesModel(HttpClient httpClient, IConfiguration configuration) : base(httpClient)
        {
            Configuration = configuration;
        }
        public string LastError { get; set; }
        public List<GanjoorPoetViewModel> Poets { get; set; }
        public GanjoorPoetViewModel Poet { get; set; }
        public List<GanjoorQuotedPoemViewModel> ClaimedQuotes { get; set; }
        public List<GanjoorQuotedPoemViewModel> Quotes { get; set; }

        private async Task<bool> preparePoets()
        {
            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poets");
            if (!response.IsSuccessStatusCode)
            {
                LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                return false;
            }
            Poets = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoetViewModel>>();

            return true;
        }
        public async Task<IActionResult> OnGetAsync()
        {
            if (bool.Parse(Configuration["MaintenanceMode"]))
            {
                return StatusCode(503);
            }

            LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Token"]);
            ViewData["GoogleAnalyticsCode"] = Configuration["GoogleAnalyticsCode"];

            //todo: use html master layout or make it partial
            if (false == (await preparePoets()))
                return Page();

            if (!string.IsNullOrEmpty(Request.Query["p"]))
            {
                var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poet?url=/{Request.Query["p"]}");
                if (!response.IsSuccessStatusCode)
                {
                    LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
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
                LastError = JsonConvert.DeserializeObject<string>(await responseQuotes.Content.ReadAsStringAsync());
                return Page();
            }

            Quotes = JArray.Parse(await responseQuotes.Content.ReadAsStringAsync()).ToObject<List<GanjoorQuotedPoemViewModel>>();

            foreach (var quote in Quotes)
            {
                var poemQuery = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poem/{quote.PoemId}");
                if (!poemQuery.IsSuccessStatusCode)
                {
                    LastError = JsonConvert.DeserializeObject<string>(await poemQuery.Content.ReadAsStringAsync());
                    return Page();
                }
                var poem = JObject.Parse(await poemQuery.Content.ReadAsStringAsync()).ToObject<GanjoorPoemCompleteViewModel>();
                quote.Poem = new RMuseum.Models.Ganjoor.GanjoorPoem()
                {
                    Id = poem.Id,
                    FullTitle = poem.FullTitle,
                    FullUrl = poem.FullUrl,
                };
            }

            var responseClaimedQuotes = await _httpClient.GetAsync(url + "&claimed=true");
            if (!responseClaimedQuotes.IsSuccessStatusCode)
            {
                LastError = JsonConvert.DeserializeObject<string>(await responseClaimedQuotes.Content.ReadAsStringAsync());
                return Page();
            }

            ClaimedQuotes = JArray.Parse(await responseClaimedQuotes.Content.ReadAsStringAsync()).ToObject<List<GanjoorQuotedPoemViewModel>>();

            foreach (var quote in ClaimedQuotes)
            {
                var poemQuery = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poem/{quote.PoemId}");
                if (!poemQuery.IsSuccessStatusCode)
                {
                    LastError = JsonConvert.DeserializeObject<string>(await poemQuery.Content.ReadAsStringAsync());
                    return Page();
                }
                var poem = JObject.Parse(await poemQuery.Content.ReadAsStringAsync()).ToObject<GanjoorPoemCompleteViewModel>();
                quote.Poem = new RMuseum.Models.Ganjoor.GanjoorPoem()
                {
                    Id = poem.Id,
                    FullTitle = poem.FullTitle,
                    FullUrl = poem.FullUrl,
                };
            }

            


            ViewData["Title"] = Poet == null ? "نقل قول‌های شاعران" : $"نقل‌قول‌ها و شعرهای مرتبط {Poet.Nickname}";

            return Page();
        }
    }
}
