using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DNTPersianUtils.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Utils;
using RSecurityBackend.Models.Generic;


namespace GanjooRazor.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class SimiModel : LoginPartialEnabledPageModel
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
        public SimiModel(HttpClient httpClient, IMemoryCache memoryCache, IConfiguration configuration) : base(httpClient)
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
        public string Metre { get; set; }
        public string Rhyme { get; set; }

        public GanjoorLanguage[] Languages { get; set; }
        public GanjoorPoetCompleteViewModel Poet { get; set; }
        public List<GanjoorPoemCompleteViewModel> Poems { get; set; }
        public string PagingToolsHtml { get; set; }
        public string LastError { get; set; }

        public string Language { get; set; }


        /// <summary>
        /// can edit
        /// </summary>
        public bool CanEdit { get; set; }

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

        /// <summary>
        /// rhythms alphabetically
        /// </summary>
        public GanjoorMetre[] RhythmsAlphabetically { get; set; }

        /// <summary>
        /// rhythms by frequency
        /// </summary>
        public GanjoorMetre[] RhythmsByVerseCount { get; set; }

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

            bool anyParamsGiven = false;

            LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Token"]);

            CanEdit = Request.Cookies["CanEdit"] == "True";

            PoetId = string.IsNullOrEmpty(Request.Query["a"]) ? 0 : int.Parse(Request.Query["a"]);

            anyParamsGiven |= PoetId != 0;

            Language = Request.Query["l"];

            anyParamsGiven |= Language != null;

            Language ??= "fa-IR";

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

            RhythmsByVerseCount = JsonConvert.DeserializeObject<GanjoorMetre[]>(await rhythmResponse.Content.ReadAsStringAsync());

            List<GanjoorMetre> rhythmsByVerseCount = new List<GanjoorMetre>(RhythmsByVerseCount);
            rhythmsByVerseCount.Sort((a, b) => a.Rhythm.CompareTo(b.Rhythm));
            rhythmsByVerseCount.Insert(0, new GanjoorMetre()
            {
                Rhythm = "null"
            }
            );
            rhythmsByVerseCount.Insert(0, new GanjoorMetre()
            {
                Rhythm = ""
            }
            );

            RhythmsAlphabetically = rhythmsByVerseCount.ToArray();

            int pageNumber = 1;
            if (!string.IsNullOrEmpty(Request.Query["page"]))
            {
                pageNumber = int.Parse(Request.Query["page"]);
            }

            Metre = Request.Query["v"];
            Rhyme = Request.Query["g"];

            anyParamsGiven |= Metre != null;
            anyParamsGiven |= Rhyme != null;

            Metre ??= "";
            Rhyme ??= "";


            if (!string.IsNullOrEmpty(Rhyme))
            {
                Rhyme = Rhyme.Replace(" ", "");
            }


            if (!anyParamsGiven)
            {
                ViewData["Title"] = $"گنجور » شعر‌ها یا ابیات مشابه";
                return Page();
            }




            string title = "شعرها یا ابیات ";

            if (PoetId != 0)
            {
                var poetInfo = Poets.Where(p => p.Id == PoetId).SingleOrDefault();
                if (poetInfo != null)
                {
                    title += $"{poetInfo.Nickname} ";
                }
            }
            if(!string.IsNullOrEmpty(Metre))
            {
                title += $"با وزن «{Metre}»";
            }

            if(!string.IsNullOrEmpty(Rhyme))
            {
                if(!string.IsNullOrEmpty(Metre))
                {
                    title += $" و";
                }
                else
                {
                    title += $"با";
                }
                title += $" حروف قافیهٔ «{Rhyme}»";
            }

            if(Language != "fa-IR")
            {
                var langModel = Languages.Where(l => l.Code == Language).FirstOrDefault();
                if (langModel != null)
                {
                    title += $"با زبان غالب «{langModel.Name}»";
                }
            }

            string url = $"{APIRoot.Url}/api/ganjoor/poems/similar?PageNumber={pageNumber}&PageSize=20&metre={Metre}&rhyme={Rhyme}&poetId={PoetId}&language={Language}";
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
                    htmlText += $"<a href=\"/simi/?v={Uri.EscapeDataString(Metre)}&amp;g={Uri.EscapeDataString(Rhyme)}&amp;page=1{authorParam}&amp;language={Language}\"><div class=\"circled-number\">۱</div></a> …";
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
                            htmlText += $"<a href=\"/simi/?v={Uri.EscapeDataString(Metre)}&amp;g={Uri.EscapeDataString(Rhyme)}&amp;page={i}{authorParam}&amp;language={Language}\"><div class=\"circled-number\">{i.ToPersianNumbers()}</div></a>{Environment.NewLine}";
                        }
                    }
                }
                if (paginationMetadata.totalPages > (paginationMetadata.currentPage + 2))
                {
                    htmlText += $"… <a href=\"/simi/?v={Uri.EscapeDataString(Metre)}&amp;g={Uri.EscapeDataString(Rhyme)}&amp;page={paginationMetadata.totalPages}{authorParam}&amp;language={Language}\"><div class=\"circled-number\">{paginationMetadata.totalPages.ToPersianNumbers()}</div></a>{Environment.NewLine}";
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
