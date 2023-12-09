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
using RSecurityBackend.Models.Generic;

namespace GanjooRazor.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class SearchModel : LoginPartialEnabledPageModel
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
        public SearchModel(HttpClient httpClient, IMemoryCache memoryCache, IConfiguration configuration) : base(httpClient)
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

        public string Query { get; set; }
        public int PoetId { get; set; }
        public int CatId { get; set; }
        public string CatFullTitle { get; set; }
        public string CatFullUrl { get; set; }
        public GanjoorPoetCompleteViewModel Poet { get; set; }
        public List<GanjoorPoemCompleteViewModel> Poems { get; set; }
        public PaginationMetadata PaginationMetadata { get; set; }
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

        public string AlternativeSearchPhrase { get; set; }

        public string AlternativeSearchPhraseDescription { get; set; }

        public bool Quoted { get; set; }

        public bool ExactSearch { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (bool.Parse(Configuration["MaintenanceMode"]))
            {
                return StatusCode(503);
            }

            LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Token"]);

            Query = Request.Query["s"].ApplyCorrectYeKe().Trim();
            ExactSearch = Request.Query["es"] == "1";
            bool quotes = Query.IndexOf("\"") != -1 || ExactSearch;
            Query = LanguageUtils.MakeTextSearchable(Query); //replace zwnj with space
            if (quotes)
                Query = $"\"{Query}\"";

            Quoted = quotes && Query.Contains(" ");
            AlternativeSearchPhrase = Quoted ? WebUtility.UrlEncode(Query.Replace("\"", "")) : Query.Contains(" ") ? WebUtility.UrlEncode($"\"{Query}\"") : "";
            AlternativeSearchPhraseDescription = "";
            if (Quoted)
            {
                AlternativeSearchPhraseDescription = "راضی نشدید؟! ";
                bool needsComma = false;
                var splitteds = Query.Replace("\"", "").Split(' ');
                for(int sp = 0; sp < splitteds.Length - 1; sp++)
                {
                    var splitted = splitteds[sp];
                    if (needsComma)
                        AlternativeSearchPhraseDescription += "، ";
                    AlternativeSearchPhraseDescription += $"«{splitted}»";
                    needsComma = true;
                }
                AlternativeSearchPhraseDescription += $" و «{splitteds[splitteds.Length - 1]}»";
                AlternativeSearchPhraseDescription += " را بدون لحاظ کردن ترتیب واژگان جستجو کنید.";
            }
            else
            if (Query.Contains(" "))
            {
                AlternativeSearchPhraseDescription = $"راضی نشدید؟! عبارت «{Query}» را به طور دقیق جستجو کنید.";
            }

            PoetId = string.IsNullOrEmpty(Request.Query["author"]) ? 0 : int.Parse(Request.Query["author"]);
            CatId = string.IsNullOrEmpty(Request.Query["cat"]) ? 0 : int.Parse(Request.Query["cat"]);

            if (CatId != 0)
            {
                var catResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/cat/{CatId}?poems=false&mainSections=false");
                if (!catResponse.IsSuccessStatusCode)
                {
                    LastError = JsonConvert.DeserializeObject<string>(await catResponse.Content.ReadAsStringAsync());
                    return Page();
                }
                else
                {
                    var cat = JObject.Parse(await catResponse.Content.ReadAsStringAsync()).ToObject<GanjoorPoetCompleteViewModel>();
                    CatFullUrl = cat.Cat.FullUrl;
                    CatFullTitle = "";
                    foreach (var parentCat in cat.Cat.Ancestors)
                    {
                        CatFullTitle += parentCat.Title;
                        CatFullTitle += " »";
                    }
                    CatFullTitle += " " + cat.Cat.Title;
                }
            }
            else
            {
                CatFullUrl = "";
                CatFullTitle = "";
            }

            ViewData["GoogleAnalyticsCode"] = Configuration["GoogleAnalyticsCode"];

            //todo: use html master layout or make it partial
            // 1. poets 
            if (false == (await preparePoets()))
                return Page();

            var poetName = Poets.SingleOrDefault(p => p.Id == PoetId);
            if (poetName != null)
            {
                if(CatFullTitle != "")
                {
                    ViewData["Title"] = $"گنجور » نتایج جستجو برای {Query} در بخش {CatFullTitle}";
                }
                else
                {
                    ViewData["Title"] = $"گنجور » نتایج جستجو برای {Query} در آثار {poetName?.Name}";
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(Query))
                {
                    ViewData["Title"] = $"گنجور » نتایج جستجو برای {Query}";
                }
                else
                {
                    ViewData["Title"] = $"گنجور » جستجو";
                }
            }


            if (PoetId != 0)
            {
                if (false == (await preparePoet()))
                    return Page();
            }

            // 2. search verses
            int pageNumber = 1;
            if (!string.IsNullOrEmpty(Request.Query["page"]))
            {
                pageNumber = int.Parse(Request.Query["page"]);
            }

            if (pageNumber > 1)
            {
                ViewData["Title"] += $" - صفحهٔ {pageNumber.ToPersianNumbers()}";
            }

            HttpResponseMessage searchQueryResponse = null;

            if (!string.IsNullOrEmpty(Query))
            {
                searchQueryResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poems/search?term={Query}&poetId={PoetId}&catId={CatId}&PageNumber={pageNumber}&PageSize=20");

                if (!searchQueryResponse.IsSuccessStatusCode)
                {
                    LastError = JsonConvert.DeserializeObject<string>(await searchQueryResponse.Content.ReadAsStringAsync());
                    return Page();
                }

                Poems = JArray.Parse(await searchQueryResponse.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoemCompleteViewModel>>();
            }


            if (Poems != null && Poems.Count == 0)
            {
                Poems = null;
            }

            if (Poems != null)
            {
                // highlight searched word
                string[] queryParts = Query.IndexOf('"') == 0 && Query.LastIndexOf('"') == (Query.Length - 1) ?
                       new string[] { Query.Replace("\"", "") }
                       :
                       Query.Replace("\"", "").Split(' ', StringSplitOptions.RemoveEmptyEntries);

                foreach (var poem in Poems)
                {
                    string[] lines = poem.PlainText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
                    List<int> linesInExcerpt = new List<int>();
                    for (int i = 0; i < lines.Length; i++)
                    {
                        foreach (var queryPart in queryParts)
                        {
                            if (lines[i].IndexOf(queryPart) != -1)
                            {
                                if (i > 0)
                                {
                                    if (linesInExcerpt.IndexOf(i - 1) == -1)
                                    {
                                        linesInExcerpt.Add(i - 1);
                                    }
                                }
                                if (linesInExcerpt.IndexOf(i) == -1)
                                {
                                    linesInExcerpt.Add(i);
                                }

                                if (i < (lines.Length - 1))
                                    linesInExcerpt.Add(i + 1);

                                break;
                            }
                        }
                    }

                    


                    string plainText = "";
                    for (int i = 0; i < linesInExcerpt.Count; i++)
                    {
                        if (linesInExcerpt[i] > 0 && linesInExcerpt.IndexOf(linesInExcerpt[i] - 1) == -1)
                            plainText += "... ";
                        plainText += $"{lines[linesInExcerpt[i]]}";
                        if (linesInExcerpt[i] < (lines.Length - 1) && linesInExcerpt.IndexOf(linesInExcerpt[i] + 1) == -1)
                            plainText += " ...";
                        plainText += $"{Environment.NewLine}";
                    }

                    string finalPlainText = "";
                    foreach (string line in plainText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
                    {
                        finalPlainText += $"<p>{line}</p>";
                    }
                    if(linesInExcerpt.Count >0 )
                    {
                        poem.PlainText = finalPlainText;
                    }

                    for (int i = 0; i < queryParts.Length; i++)
                    {
                        string cssClass = i % 3 == 0 ? "hilite" : i % 3 == 1 ? "hilite2" : "hilite3";
                        poem.PlainText = Regex.Replace(poem.PlainText, queryParts[i], $"<span class=\"{cssClass}\">{queryParts[i]}</span>", RegexOptions.IgnoreCase | RegexOptions.RightToLeft); ;
                    }


                }

                if (searchQueryResponse != null)
                {
                    string paginationMetadataJsonValue = searchQueryResponse.Headers.GetValues("paging-headers").FirstOrDefault();

                    if (!string.IsNullOrEmpty(paginationMetadataJsonValue))
                    {
                        PaginationMetadata = JsonConvert.DeserializeObject<PaginationMetadata>(paginationMetadataJsonValue);
                        string catQuery = "";
                        if (!string.IsNullOrEmpty(Request.Query["cat"]))
                        {
                            catQuery = $"&cat={Request.Query["cat"]}";
                        }
                        PagingToolsHtml = GeneratePagingBarHtml(PaginationMetadata, $"/search?s={WebUtility.UrlEncode(Query)}&amp;author={PoetId}{catQuery}");
                    }
                }


            }



            return Page();
        }

        private string GeneratePagingBarHtml(PaginationMetadata paginationMetadata, string routeStartWithQueryStrings)
        {
            string htmlText = $"<div>{Environment.NewLine}";


            if (paginationMetadata != null && paginationMetadata.totalPages > 1)
            {
                if (paginationMetadata.currentPage > 3)
                {
                    htmlText += $"<a href=\"{routeStartWithQueryStrings.Replace("\"", "%22")}&amp;page=1\"><div class=\"circled-number\">۱</div></a>{Environment.NewLine} …";
                }
                for (int i = paginationMetadata.currentPage - 2; i <= (paginationMetadata.currentPage + 2); i++)
                {
                    if (i >= 1 && i <= paginationMetadata.totalPages)
                    {
                        if (i == paginationMetadata.currentPage)
                        {
                            htmlText += $"<div class=\"circled-number-diff\">{i.ToPersianNumbers()}</div>";
                        }
                        else
                        {
                            htmlText += $"<a href=\"{routeStartWithQueryStrings.Replace("\"", "%22")}&amp;page={i}\"><div class=\"circled-number\">{i.ToPersianNumbers()}</div></a>{Environment.NewLine}";
                        }
                    }
                }
                if (paginationMetadata.totalPages > (paginationMetadata.currentPage + 2))
                {
                    htmlText += $"… <a href=\"{routeStartWithQueryStrings.Replace("\"", "%22")}&amp;page={paginationMetadata.totalPages}\"><div class=\"circled-number\">{paginationMetadata.totalPages.ToPersianNumbers()}</div></a>{Environment.NewLine}";
                }
            }

            htmlText += $"</div>{Environment.NewLine}";
            return htmlText;
        }

    }

}
