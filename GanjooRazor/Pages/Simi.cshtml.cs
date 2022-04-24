using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DNTPersianUtils.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Services.Implementation;
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
        public GanjoorPoetCompleteViewModel Poet { get; set; }
        public List<GanjoorPoemCompleteViewModel> Poems { get; set; }
        public string PagingToolsHtml { get; set; }
        public string LastError { get; set; }

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

        public async Task<IActionResult> OnGetAsync()
        {
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

            if (string.IsNullOrEmpty(Request.Query["v"]) || string.IsNullOrEmpty(Request.Query["g"]))
            {
                LastError = "<p>موردی با مشخصات انتخاب شده یافت نشد.</p>";
                return Page();
            }

            int pageNumber = 1;
            if (!string.IsNullOrEmpty(Request.Query["page"]))
            {
                pageNumber = int.Parse(Request.Query["page"]);
            }

            Metre = Request.Query["v"];
            Rhyme = Request.Query["g"];

            string title = "شعرها یا ابیات ";

            if (PoetId != 0)
            {
                var poetInfo = Poets.Where(p => p.Id == PoetId).SingleOrDefault();
                if (poetInfo != null)
                {
                    title += $"{poetInfo.Nickname} ";
                }
            }
            title += $"با وزن «{Metre}»";
            title += $" و حروف قافیهٔ «{Rhyme}»";

            ViewData["Title"] = $"گنجور » {title}";


            string url = $"{APIRoot.Url}/api/ganjoor/poems/similar?PageNumber={pageNumber}&PageSize=20&metre={Metre}&rhyme={Rhyme}&poetId={PoetId}";
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
                string authorParam = PoetId != 0 ? $"&amp;a={PoetId}" : "";
                title += $" - صفحهٔ {pageNumber.ToPersianNumbers()}";
                if (paginationMetadata.currentPage > 3)
                {
                    htmlText += $"[<a href=\"/simi/?v={Uri.EscapeDataString(Metre)}&amp;g={Uri.EscapeDataString(Rhyme)}&amp;page=1{authorParam}\">صفحهٔ اول</a>] …";
                }
                for (int i = (paginationMetadata.currentPage - 2); i <= (paginationMetadata.currentPage + 2); i++)
                {
                    if (i >= 1 && i <= paginationMetadata.totalPages)
                    {
                        htmlText += " [";
                        if (i == paginationMetadata.currentPage)
                        {
                            htmlText += i.ToPersianNumbers();
                        }
                        else
                        {
                            htmlText += $"<a href=\"/simi/?v={Uri.EscapeDataString(Metre)}&amp;g={Uri.EscapeDataString(Rhyme)}&amp;page={i}{authorParam}\">{i.ToPersianNumbers()}</a>";
                        }
                        htmlText += "] ";
                    }
                }
                if (paginationMetadata.totalPages > (paginationMetadata.currentPage + 2))
                {
                    htmlText += $"… [<a href=\"/simi/?v={Uri.EscapeDataString(Metre)}&amp;g={Uri.EscapeDataString(Rhyme)}&amp;page={paginationMetadata.totalPages}{authorParam}\">صفحهٔ آخر</a>]";
                }
            }
            PagingToolsHtml = htmlText;

            return Page();
        }
        public string HtmlText { get; set; }
        
    }
}
