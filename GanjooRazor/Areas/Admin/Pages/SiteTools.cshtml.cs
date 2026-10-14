using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Auth.ViewModels;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GanjooRazor.Areas.Admin.Pages
{
    /// <summary>
    /// site-wide maintenance tools (rebuild sitemaps/stats/public data, Tajik transliteration, per-poet
    /// jobs ...), grouped by topic - moved here from the "ابزارها" card of ModifyPage. Every action is a
    /// POST handler that starts a background job; progress is on the LongRunningJobs page.
    /// </summary>
    public class SiteToolsModel : PageModel
    {
        private readonly HttpClient _httpClient;

        public SiteToolsModel(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public string LastMessage { get; set; }

        /// <summary>
        /// poets for the per-poet tools
        /// </summary>
        public List<GanjoorPoetViewModel> Poets { get; set; } = new List<GanjoorPoetViewModel>();

        /// <summary>
        /// preselected poet (ModifyPage links here with ?poetId=)
        /// </summary>
        public int PoetId { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(Request.Cookies["Token"]))
                return Redirect("/");

            int.TryParse(Request.Query["poetId"], out var preselectedPoet);
            PoetId = preselectedPoet;

            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poets/secure");
                    if (!response.IsSuccessStatusCode)
                    {
                        LastMessage = await global::GanjooRazor.Utils.ApiErrorReader.ReadErrorAsync(response);
                        return Page();
                    }
                    Poets = new List<GanjoorPoetViewModel>(JsonConvert.DeserializeObject<GanjoorPoetViewModel[]>(await response.Content.ReadAsStringAsync()));
                }
                else
                {
                    LastMessage = "لطفاً از گنجور خارج و مجددا به آن وارد شوید.";
                }
            }
            return Page();
        }

        public async Task<IActionResult> OnPostRebuildSitemapAsync()
        {
            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PostAsync($"{APIRoot.Url}/api/ganjoor/sitemap", null);
                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest(await global::GanjooRazor.Utils.ApiErrorReader.ReadErrorAsync(response));
                    }
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }

        public async Task<IActionResult> OnPostRebuildTajikSitemapAsync()
        {
            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PostAsync($"{APIRoot.Url}/api/ganjoor/sitemap/tajik", null);
                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest(await global::GanjooRazor.Utils.ApiErrorReader.ReadErrorAsync(response));
                    }
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }


        public async Task<IActionResult> OnPostRebuildStatsAsync()
        {
            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/rebuild/stats", null);
                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest(await global::GanjooRazor.Utils.ApiErrorReader.ReadErrorAsync(response));
                    }
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }

        /// <summary>
        /// (re)generate the public git-tracked data export (poets/categories/poems as JSON,
        /// pushed to the configured git remote) — runs as a background job, same as the other
        /// rebuild/regenerate tools on this page; check the Jobs page for progress
        /// </summary>
        public async Task<IActionResult> OnPostRegeneratePublicDataAsync()
        {
            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PostAsync($"{APIRoot.Url}/api/ganjoor/publicdata/batchexport", null);
                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest(await global::GanjooRazor.Utils.ApiErrorReader.ReadErrorAsync(response));
                    }
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }

        /// <summary>
        /// (re)generate the separate Tajik (Cyrillic) public data export — same background-job
        /// pattern as the main one, different repo
        /// </summary>
        public async Task<IActionResult> OnPostRegenerateTajikPublicDataAsync()
        {
            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PostAsync($"{APIRoot.Url}/api/ganjoor/tajik/publicdata/batchexport", null);
                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest(await global::GanjooRazor.Utils.ApiErrorReader.ReadErrorAsync(response));
                    }
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }

        
        public async Task<IActionResult> OnPostRefillSectionsCoupletCountsAsync()
        {
            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PostAsync($"{APIRoot.Url}/api/ganjoor/fillsectioncoupletcounts", null);
                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest(await global::GanjooRazor.Utils.ApiErrorReader.ReadErrorAsync(response));
                    }
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }




        public async Task<IActionResult> OnPostRebuildRelatedPagesAsync()
        {
            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/quoted/pages/generate", null);
                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest(await global::GanjooRazor.Utils.ApiErrorReader.ReadErrorAsync(response));
                    }
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }

        public async Task<IActionResult> OnPostRebuildDigitalSourcesStatsAsync()
        {
            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PostAsync($"{APIRoot.Url}/api/ganjoor/source/stats/rebuild", null);
                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest(await global::GanjooRazor.Utils.ApiErrorReader.ReadErrorAsync(response));
                    }
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }

        public async Task<IActionResult> OnPostTransilerateAsync()
        {
            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/transilerate", null);
                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest(await global::GanjooRazor.Utils.ApiErrorReader.ReadErrorAsync(response));
                    }
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }


        


        public async Task<IActionResult> OnPostRebuildWordCountsAsync(int poetId)
        {
            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PostAsync($"{APIRoot.Url}/api/ganjoor/wordcounts/rebuild/{poetId}", null);
                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest(await global::GanjooRazor.Utils.ApiErrorReader.ReadErrorAsync(response));
                    }
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }

        public async Task<IActionResult> OnPostImportNaskbanPaperSourcesAsync(int poetId, string username, string password)
        {
            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    LoginViewModel model = new LoginViewModel()
                    {
                        Username = username,
                        Password = password,
                        ClientAppName = "GanjooRazor",
                        Language = "fa-IR"
                    };
                    var stringContent = new StringContent(JsonConvert.SerializeObject(model), Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/naskban/import/poetbooks/{poetId}", stringContent);
                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest(await global::GanjooRazor.Utils.ApiErrorReader.ReadErrorAsync(response));
                    }
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }



        public async Task<IActionResult> OnPostImportMuseumPaperSourcesAsync(int poetId)
        {
            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/papersources/import/{poetId}", null);
                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest(await global::GanjooRazor.Utils.ApiErrorReader.ReadErrorAsync(response));
                    }
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }


        /// <summary>
        /// starts one of the (resumable) open ai jobs
        /// </summary>
        public async Task<IActionResult> OnPostRunAiJobAsync(string job, int masterCatId, int startFrom, int count, bool regenerateAI, bool restart, bool dryRun)
        {
            string query;
            string common = $"count={count}&restart={restart.ToString().ToLowerInvariant()}";
            string path;
            switch (job)
            {
                case "couplet-summaries":
                    path = "ai/generate/summaries";
                    query = $"startFrom={startFrom}&masterCatId={masterCatId}&regenerateAI={regenerateAI.ToString().ToLowerInvariant()}&{common}";
                    break;
                case "poem-summaries":
                    path = "ai/generate/poem/summaries";
                    query = $"startFrom={startFrom}&masterCatId={masterCatId}&regenerateAI={regenerateAI.ToString().ToLowerInvariant()}&{common}";
                    break;
                case "geo":
                    path = "ai/generate/poem/geo";
                    query = $"startFrom={startFrom}&{common}";
                    break;
                case "titles":
                    path = "ai/generate/poem/titles";
                    query = $"masterCatId={masterCatId}&startFrom={startFrom}&{common}";
                    break;
                case "people":
                    path = "ai/generate/poem/people";
                    query = $"masterCatId={masterCatId}&startPoemId={startFrom}&dryRun={dryRun.ToString().ToLowerInvariant()}&{common}";
                    break;
                default:
                    return BadRequest("unknown job");
            }

            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/{path}?{query}", null);
                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest(await global::GanjooRazor.Utils.ApiErrorReader.ReadErrorAsync(response));
                    }
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }

    }
}
