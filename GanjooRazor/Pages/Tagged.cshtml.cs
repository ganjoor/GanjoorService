using DNTPersianUtils.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Models.Ganjoor;
using RMuseum.Utils;
using RSecurityBackend.Models.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System;

namespace GanjooRazor.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class TaggedModel : LoginPartialEnabledPageModel
    {
        /// <summary>
        /// memory cache
        /// </summary>
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// configration file reader (appsettings.json)
        /// </summary>
        private readonly IConfiguration Configuration;


        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="memoryCache"></param>
        /// <param name="configuration"></param>
        public TaggedModel(HttpClient httpClient, IMemoryCache memoryCache, IConfiguration configuration) : base(httpClient)
        {
            _memoryCache = memoryCache;
            Configuration
                = configuration;
        }

        /// <summary>
        /// aggressive cache
        /// </summary>
        public bool AggressiveCacheEnabled
        {
            get
            {
                try
                {
                    return bool.Parse(Configuration["AggressiveCacheEnabled"]);
                }
                catch
                {
                    return false;
                }
            }
        }

        public List<GanjoorPoetViewModel> Poets { get; set; }
        public int PoetId { get; set; }
        public GanjoorPoetCompleteViewModel Poet { get; set; }
        public List<GanjoorPoemCompleteViewModel> Poems { get; set; }
        public string PagingToolsHtml { get; set; }
        public string LastError { get; set; }
        public string Language { get; set; }

        public GanjoorLanguage[] Languages { get; set; }

        private async Task ReadLanguagesAsync()
        {
            HttpResponseMessage response = await _httpClient.GetAsync($"{APIRoot.Url}/api/translations/languages");
            if (!response.IsSuccessStatusCode)
            {
                LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                return;
            }

            Languages = JsonConvert.DeserializeObject<GanjoorLanguage[]>(await response.Content.ReadAsStringAsync());
        }

        private async Task<bool> preparePoets()
        {
            var cacheKey = $"/api/ganjoor/poets";
            if (!_memoryCache.TryGetValue(cacheKey, out List<GanjoorPoetViewModel> poets))
            {
                var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poets");
                if (!response.IsSuccessStatusCode)
                {
                    LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                    return false;
                }
                poets = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoetViewModel>>();
                if (AggressiveCacheEnabled)
                {
                    _memoryCache.Set(cacheKey, poets);
                }
            }

            Poets = poets;

            await ReadLanguagesAsync();
            return true;
        }

        private async Task<bool> preparePoet()
        {
            var cacheKey = $"/api/ganjoor/poet/{PoetId}";
            if (!_memoryCache.TryGetValue(cacheKey, out GanjoorPoetCompleteViewModel poet))
            {
                var poetResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poet/{PoetId}");
                if (!poetResponse.IsSuccessStatusCode)
                {
                    LastError = JsonConvert.DeserializeObject<string>(await poetResponse.Content.ReadAsStringAsync());
                    return false;
                }
                poet = JObject.Parse(await poetResponse.Content.ReadAsStringAsync()).ToObject<GanjoorPoetCompleteViewModel>();
                if (AggressiveCacheEnabled)
                {
                    _memoryCache.Set(cacheKey, poet);
                }
            }

            Poet = poet;
            return true;
        }

        public async Task<IActionResult> OnGetPoetInformationAsync(int id)
        {
            if (id == 0)
                return new OkObjectResult(null);
            var cacheKey = $"/api/ganjoor/poet/{id}";
            if (!_memoryCache.TryGetValue(cacheKey, out GanjoorPoetCompleteViewModel poet))
            {
                var poetResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poet/{id}");
                if (!poetResponse.IsSuccessStatusCode)
                {
                    return BadRequest(JsonConvert.DeserializeObject<string>(await poetResponse.Content.ReadAsStringAsync()));
                }
                poet = JObject.Parse(await poetResponse.Content.ReadAsStringAsync()).ToObject<GanjoorPoetCompleteViewModel>();
                if (AggressiveCacheEnabled)
                {
                    _memoryCache.Set(cacheKey, poet);
                }
            }
            return new OkObjectResult(poet);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (bool.Parse(Configuration["MaintenanceMode"]))
            {
                return StatusCode(503);
            }

            LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Token"]);


            PoetId = string.IsNullOrEmpty(Request.Query["a"]) ? 0 : int.Parse(Request.Query["a"]);

            ViewData["GoogleAnalyticsCode"] = Configuration["GoogleAnalyticsCode"];

            //todo: use html master layout or make it partial
            // 1. poets 
            if (false == (await preparePoets()))
                return Page();

            if (PoetId != 0)
            {
                if (false == (await preparePoet()))
                    return Page();
            }

            // 2. search verses



            var rhythmResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/rhythms?sortOnVerseCount=true");
            if (!rhythmResponse.IsSuccessStatusCode)
            {
                LastError = JsonConvert.DeserializeObject<string>(await rhythmResponse.Content.ReadAsStringAsync());
                return Page();
            }


            int pageNumber = 1;
            if (!string.IsNullOrEmpty(Request.Query["page"]))
            {
                pageNumber = int.Parse(Request.Query["page"]);
            }

            Language = Request.Query["l"];

            Language ??= "fa-IR";



            string title = "شعرها یا ابیات ";

            if (PoetId != 0)
            {
                var poetInfo = Poets.Where(p => p.Id == PoetId).SingleOrDefault();
                if (poetInfo != null)
                {
                    title += $"{poetInfo.Nickname} ";
                }
            }
            var langModel = Languages.Where(l => l.Code == Language).FirstOrDefault();
            if(langModel != null)
            {
                title += $"با زبان غالب «{langModel.Name}»";
            }
            


            string url = $"{APIRoot.Url}/api/ganjoor/sections/tagged/language?PageNumber={pageNumber}&PageSize=20&language={Language}&poetId={PoetId}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                return Page();
            }

            Poems = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoemCompleteViewModel>>();

            foreach (var poem in Poems)
            {
                poem.HtmlText = GanjoorPoemTools.GetPoemHtmlExcerpt(poem.HtmlText);
            }

            string paginnationMetadata = response.Headers.GetValues("paging-headers").FirstOrDefault();
            PaginationMetadata paginationMetadata = JsonConvert.DeserializeObject<PaginationMetadata>(paginnationMetadata);

            string htmlText = "";
            if (paginationMetadata.totalPages > 1)
            {
                htmlText = $"<div>{Environment.NewLine}";
                string authorParam = PoetId != 0 ? $"&amp;a={PoetId}" : "";
                title += $" - صفحهٔ {pageNumber.ToPersianNumbers()}";
                if (paginationMetadata.currentPage > 3)
                {
                    htmlText += $"<a href=\"/tagged/?l={Uri.EscapeDataString(Language)}&amp;page=1{authorParam}\"><div class=\"circled-number\">۱</div></a> …";
                }
                for (int i = paginationMetadata.currentPage - 2; i <= (paginationMetadata.currentPage + 2); i++)
                {
                    if (i >= 1 && i <= paginationMetadata.totalPages)
                    {
                        if (i == paginationMetadata.currentPage)
                        {
                            htmlText += $"<div class=\"circled-number-diff\">{i.ToPersianNumbers()}</div>{Environment.NewLine}";
                        }
                        else
                        {
                            htmlText += $"<a href=\"/tagged/?l={Uri.EscapeDataString(Language)}&amp;page={i}{authorParam}\"><div class=\"circled-number\">{i.ToPersianNumbers()}</div></a>{Environment.NewLine}";
                        }
                    }
                }
                if (paginationMetadata.totalPages > (paginationMetadata.currentPage + 2))
                {
                    htmlText += $"… <a href=\"/tagged/?l={Uri.EscapeDataString(Language)}&amp;page={paginationMetadata.totalPages}{authorParam}\"><div class=\"circled-number\">{paginationMetadata.totalPages.ToPersianNumbers()}</div></a>{Environment.NewLine}";
                }
                htmlText += $"</div>{Environment.NewLine}";
            }

            ViewData["Title"] = $"گنجور » {title}";
            PagingToolsHtml = htmlText;

            return Page();
        }
        public string HtmlText { get; set; }

    }
}
