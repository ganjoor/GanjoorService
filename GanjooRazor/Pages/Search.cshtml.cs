using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DNTPersianUtils.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;

namespace GanjooRazor.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]

    public class SearchModel : PageModel
    {
        /// <summary>
        /// IMemoryCache
        /// </summary>
        protected readonly IMemoryCache _memoryCache;

        /// <summary>
        /// HttpClient instance
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="memoryCache"></param>
        /// <param name="httpClient"></param>
        public SearchModel(IMemoryCache memoryCache, HttpClient httpClient)
        {
            _memoryCache = memoryCache;
            _httpClient = httpClient;
        }

        public List<GanjoorPoetViewModel> Poets { get; set; }

        public string Query { get; set; }
        public int PoetId { get; set; }
        public int CatId { get; set; }
        public GanjoorPoetCompleteViewModel Poet { get; set; }
        public List<GanjoorPoemCompleteViewModel> Poems { get; set; }
        public string PagingToolsHtml { get; set; }

        private async Task preparePoets(bool includeBio)
        {
            var cacheKey = $"/api/ganjoor/poets?includeBio={includeBio}";
            if (!_memoryCache.TryGetValue(cacheKey, out List<GanjoorPoetViewModel> poets))
            {
                var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poets?includeBio={includeBio}");
                response.EnsureSuccessStatusCode();
                poets = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoetViewModel>>();

                _memoryCache.Set(cacheKey, poets);
            }

            Poets = poets;
        }

        private async Task preparePoet()
        {
            var cacheKey = $"/api/ganjoor/poet/{PoetId}";
            if (!_memoryCache.TryGetValue(cacheKey, out GanjoorPoetCompleteViewModel poet))
            {
                var poetResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poet/{PoetId}");
                poetResponse.EnsureSuccessStatusCode();
                poet = JObject.Parse(await poetResponse.Content.ReadAsStringAsync()).ToObject<GanjoorPoetCompleteViewModel>();
                _memoryCache.Set(cacheKey, poet);
            }

            Poet = poet;
        }

        public async Task<IActionResult> OnGet()
        {
            Query = Request.Query["s"].ApplyCorrectYeKe().Trim();
            PoetId = string.IsNullOrEmpty(Request.Query["author"]) ? 0 : int.Parse(Request.Query["author"]);
            CatId = string.IsNullOrEmpty(Request.Query["cat"]) ? 0 : int.Parse(Request.Query["cat"]);

            //todo: use html master layout or make it partial
            // 1. poets 
            await preparePoets(false);

            var poetName = Poets.SingleOrDefault(p => p.Id == PoetId);
            if (poetName != null)
            {
                ViewData["Title"] = $"گنجور &raquo; نتایج جستجو برای {Query} &raquo; {poetName?.Name}";
            }
            else
            {
                ViewData["Title"] = $"گنجور &raquo; نتایج جستجو برای {Query}";
            }

            if (PoetId != 0)
            {
                await preparePoet();

            }

            // 2. search verses
            int pageNumber = 1;
            if (!string.IsNullOrEmpty(Request.Query["page"]))
            {
                pageNumber = int.Parse(Request.Query["page"]);
            }

            var searchQueryResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poems/search?term={Query}&poetId={PoetId}&catId={CatId}&PageNumber={pageNumber}&PageSize=20");

            searchQueryResponse.EnsureSuccessStatusCode();

            Poems = JArray.Parse(await searchQueryResponse.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoemCompleteViewModel>>();
            if (Poems != null)
            {
                // highlight searched word
                foreach (var poem in Poems)
                {
                    string[] queryParts = Query.Replace("\"", "").Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    int firstIndex = poem.PlainText.Length;
                    for (int i = 0; i < queryParts.Length; i++)
                    {
                        if (poem.PlainText.IndexOf(queryParts[i]) < firstIndex)
                        {
                            if (firstIndex >= 0)
                            {
                                firstIndex = poem.PlainText.IndexOf(queryParts[i]);
                            }
                        }
                    }



                    if (firstIndex < 0)
                        firstIndex = 0;
                    _preparePoemExcerpt(poem, firstIndex);



                    for (int i = 0; i < queryParts.Length; i++)
                    {
                        string cssClass = i % 3 == 0 ? "hilite" : i % 3 == 1 ? "hilite2" : "hilite3";
                        poem.PlainText = Regex.Replace(poem.PlainText, queryParts[i], $"<span class=\"{cssClass}\">{queryParts[i]}</span>", RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
                    }


                }

                string paginationMetadata = searchQueryResponse.Headers.GetValues("paging-headers").FirstOrDefault();

                PagingToolsHtml = GeneratePagingBarHtml(paginationMetadata, $"/search?s={Query}&author={PoetId}");
            }

            return Page();
        }

        private void _preparePoemExcerpt(GanjoorPoemCompleteViewModel poem, int leastIndex)
        {
            if (poem == null)
            {
                return;
            }
            if (leastIndex > 10)
            {
                leastIndex -= 10;
            }
            poem.PlainText = "..." + poem.PlainText.Substring(leastIndex);

            if (poem.PlainText.Length > 300)
            {
                poem.PlainText = poem.PlainText.Substring(0, 250);
                int n = poem.PlainText.LastIndexOf(' ');
                if (n >= 0)
                {
                    poem.PlainText = poem.PlainText.Substring(0, n) + " ...";
                }
                else
                {
                    poem.PlainText += "...";
                }
            }
        }

        private string GeneratePagingBarHtml(string paginationMetadataJsonValue, string routeStartWithQueryStrings)
        {
            string htmlText = "<p style=\"text-align: center;\">";

            if (!string.IsNullOrEmpty(paginationMetadataJsonValue))
            {
                PaginationMetadata paginationMetadata = JsonConvert.DeserializeObject<PaginationMetadata>(paginationMetadataJsonValue);

                if (paginationMetadata.totalPages > 1)
                {
                    if (paginationMetadata.currentPage > 3)
                    {
                        htmlText += $"[<a href=\"{routeStartWithQueryStrings}&page=1\">صفحهٔ اول</a>] …";
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
                                htmlText += $"<a href=\"{routeStartWithQueryStrings}&page={i}\">{i.ToPersianNumbers()}</a>";
                            }
                            htmlText += "] ";
                        }
                    }
                    if (paginationMetadata.totalPages > (paginationMetadata.currentPage + 2))
                    {
                        htmlText += $"… [<a href=\"{routeStartWithQueryStrings}&page={paginationMetadata.totalPages}\">صفحهٔ آخر</a>]";
                    }
                }
            }

            htmlText += $"</p>{Environment.NewLine}";
            return htmlText;
        }
    }

}
