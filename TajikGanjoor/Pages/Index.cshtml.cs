using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Models.Ganjoor;
using System.Net;
using System.Text.RegularExpressions;
using RSecurityBackend.Models.Generic;



namespace TajikGanjoor.Pages
{
    public class IndexModel : PageModel
    {
        public bool IsHomePage { get; set; }
        public bool IsSearchPage { get; set; }
        public List<GanjoorPoetViewModel>? Poets { get; set; }
        public bool IsPoetPage { get; set; }
        public bool IsCatPage { get; set; }
        public bool IsPoemPage { get; set; }
        public GanjoorPageCompleteViewModel? GanjoorPage { get; set; }
        public string NextUrl { get; set; }
        public string NextTitle { get; set; }
        public string PreviousUrl { get; set; }
        public string PreviousTitle { get; set; }
        public string BreadCrumpUrls { get; set; }
        public string Query { get; set; }
        public PaginationMetadata PaginationMetadata { get; set; }
        public string PagingToolsHtml { get; set; }

        public List<GanjoorPoemCompleteViewModel> Poems { get; set; }
        public async Task<IActionResult> OnGetAsync()
        {
            if (bool.Parse(Configuration["MaintenanceMode"] ?? false.ToString()))
            {
                return StatusCode(503);
            }


            if (false == await PreparePoetsAsync())
            {
                return Page();
            }
            IsSearchPage = !string.IsNullOrEmpty(Request.Query["s"]);
            IsHomePage = !IsSearchPage && Request.Path == "/";
            int pageNumber = 1;
            if (IsSearchPage)
            {
                Query = Request.Query["s"].ToString().Trim();
               
                if (!string.IsNullOrEmpty(Request.Query["page"]))
                {
                    pageNumber = int.Parse(Request.Query["page"]);
                }

                HttpResponseMessage searchQueryResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/tajik/search?term={Query}&PageNumber={pageNumber}&PageSize=20");

                if (!searchQueryResponse.IsSuccessStatusCode)
                {
                    LastError = JsonConvert.DeserializeObject<string>(await searchQueryResponse.Content.ReadAsStringAsync());
                    return Page();
                }

                Poems = JArray.Parse(await searchQueryResponse.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoemCompleteViewModel>>();
                if (Poems != null && Poems.Count == 0)
                {
                    Poems = null;
                }

                if (Poems != null)
                {
                    // highlight searched word
                    string[] queryParts = Query.IndexOf('"') == 0 && Query.LastIndexOf('"') == (Query.Length - 1) ?
                           [Query.Replace("\"", "")]
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
                        if (linesInExcerpt.Count > 0)
                        {
                            poem.PlainText = finalPlainText;
                        }

                        for (int i = 0; i < queryParts.Length; i++)
                        {
                            string cssClass = i % 3 == 0 ? "hilite" : i % 3 == 1 ? "hilite2" : "hilite3";
                            poem.PlainText = Regex.Replace(poem.PlainText, queryParts[i], $"<span class=\"{cssClass}\">{queryParts[i]}</span>", RegexOptions.IgnoreCase | RegexOptions.RightToLeft); ;
                        }


                    }


                    string paginationMetadataJsonValue = searchQueryResponse.Headers.GetValues("paging-headers").FirstOrDefault();

                    if (!string.IsNullOrEmpty(paginationMetadataJsonValue))
                    {
                        PaginationMetadata = JsonConvert.DeserializeObject<PaginationMetadata>(paginationMetadataJsonValue);
                        string catQuery = "";
                        if (!string.IsNullOrEmpty(Request.Query["cat"]))
                        {
                            catQuery = $"&cat={Request.Query["cat"]}";
                        }
                        PagingToolsHtml = GeneratePagingBarHtml(PaginationMetadata, $"/?s={WebUtility.UrlEncode(Query)}");
                    }

                }
            }
            else
            if (!IsHomePage)
            {
                var pageQuery = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/tajik/page?url={Request.Path}&catPoems=true");
                if (!pageQuery.IsSuccessStatusCode)
                {
                    if (pageQuery.StatusCode == HttpStatusCode.NotFound)
                    {
                        var redirectQuery = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/redirecturl?url={Request.Path}");
                        if (redirectQuery.IsSuccessStatusCode)
                        {
                            var redirectUrl = JsonConvert.DeserializeObject<string>(await redirectQuery.Content.ReadAsStringAsync());
                            return Redirect(redirectUrl ?? "/");
                        }
                        return NotFound();
                    }
                }
                if (!pageQuery.IsSuccessStatusCode)
                {
                    LastError = JsonConvert.DeserializeObject<string>(await pageQuery.Content.ReadAsStringAsync());
                    return Page();
                }
                GanjoorPage = JObject.Parse(await pageQuery.Content.ReadAsStringAsync()).ToObject<GanjoorPageCompleteViewModel>();
                if (GanjoorPage == null)
                {
                    LastError = "GanjoorPage == null";
                    return Page();
                }
                switch (GanjoorPage.GanjoorPageType)
                {
                    case GanjoorPageType.PoemPage:
                        GanjoorPage.PoetOrCat = GanjoorPage.Poem.Category;
                        PrepareNextPre();
                        IsPoemPage = true;
                        break;
                    case GanjoorPageType.PoetPage:
                        IsPoetPage = true;
                        break;
                    case GanjoorPageType.CatPage:
                        PrepareNextPre();
                        IsCatPage = true;
                        break;
                }

            }

            BreadCrumpUrls = "<a href=\"/\">Ганҷур</a>";
            if (IsSearchPage)
            {
                ViewData["Title"] = $"Ганҷур - ҷустуҷӯ - {Query}";
                if(pageNumber > 1)
                {
                    ViewData["Title"] += "Сафҳа ӣ " + pageNumber.ToString();
                }
            }
            else
            if (IsHomePage)
            {
                ViewData["Title"] = "Ганҷур";
            }
            else
            if (IsPoetPage)
            {
                ViewData["Title"] = $"Ганҷур - {GanjoorPage?.PoetOrCat.Poet.Nickname}";
                BreadCrumpUrls += $" - <a href=\"{GanjoorPage?.PoetOrCat.Poet.FullUrl}\">{GanjoorPage?.PoetOrCat.Poet.Nickname}</a>";
            }
            else
            if (IsCatPage || IsPoemPage)
            {
                string title = $"Ганҷур - ";
                foreach (var gran in GanjoorPage.PoetOrCat.Cat.Ancestors)
                {
                    title += $"{gran.Title} - ";
                    BreadCrumpUrls += $" - <a href=\"{gran.FullUrl}\">{gran.Title}</a>";
                }
                title += GanjoorPage.PoetOrCat.Cat.Title;
                BreadCrumpUrls += $" - <a href=\"{GanjoorPage?.PoetOrCat.Cat.FullUrl}\">{GanjoorPage?.PoetOrCat.Cat.Title}</a>";
                if (IsPoemPage)
                {
                    title += GanjoorPage.Poem.Title;
                    BreadCrumpUrls += $" - <a href=\"{GanjoorPage?.FullUrl}\">{GanjoorPage?.Poem.Title}</a>";
                }
                ViewData["Title"] = title;
            }
            ViewData["BreadCrumpUrls"] = BreadCrumpUrls;
            ViewData["TrackingScript"] = Configuration["TrackingScript"] != null && string.IsNullOrEmpty(Request.Cookies["Token"]) ? Configuration["TrackingScript"].Replace("loggedon", "") : Configuration["TrackingScript"];
            ViewData["GoogleTranslateLink"] = $"https://translate.google.com/translate?hl=fa&sl=fa&tl=tg&u={Uri.EscapeDataString(stringToEscape: $"https://ganjoor.net{Request.Path}")}";
            ViewData["NextUrl"] = NextUrl;
            ViewData["PreviousUrl"] = PreviousUrl;
            return Page();
        }

        private async Task<bool> PreparePoetsAsync()
        {
            var cacheKey = $"/api/ganjoor/poets";
            if (!_memoryCache.TryGetValue(cacheKey, out List<GanjoorPoetViewModel>? poets))
            {
                try
                {
                    var res1 = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poets");
                    if (!res1.IsSuccessStatusCode)
                    {
                        LastError = JsonConvert.DeserializeObject<string>(await res1.Content.ReadAsStringAsync()) ?? "!res1.IsSuccessStatusCode";
                        return false;
                    }
                    var ganjoorPoets = JArray.Parse(await res1.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoetViewModel>>();
                    ganjoorPoets = ganjoorPoets ?? [];

                    var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/tajik/poets");
                    if (!response.IsSuccessStatusCode)
                    {
                        LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()) ?? "!response.IsSuccessStatusCode";
                        return false;
                    }
                    var tajkPoets = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorTajikPoet>>();

                    poets = new List<GanjoorPoetViewModel>();
                    foreach (var tajikPoet in tajkPoets)
                    {
                        var poet = ganjoorPoets.Where(p => p.Id == tajikPoet.Id).Single();
                        poet.Nickname = tajikPoet.TajikNickname;
                        poet.Description = tajikPoet.TajikDescription;
                        poets.Add(poet);
                        
                    }
                    Poets = poets;
                    
                    if (AggressiveCacheEnabled)
                    {
                        _memoryCache.Set(cacheKey, poets);
                    }
                }
                catch (Exception e)
                {
                    LastError = e.ToString();
                    return false;
                }

            }

            Poets = poets ?? [];
            return true;
        }
        private void PrepareNextPre()
        {
            if (GanjoorPage == null) return;
            switch (GanjoorPage.GanjoorPageType)
            {
                case GanjoorPageType.PoemPage:
                    {
                        if (GanjoorPage.Poem.Next != null)
                        {
                            NextUrl = GanjoorPage.PoetOrCat.Cat.FullUrl + "/" + GanjoorPage.Poem.Next.UrlSlug;
                            NextTitle = GanjoorPage.Poem.Next.Title + ": " + GanjoorPage.Poem.Next.Excerpt;
                        }
                        else
                        if (GanjoorPage.Poem.MixedModeOrder > 0
                            && GanjoorPage.Poem.Category.Cat.Children.Where(c => c.MixedModeOrder == 0 || c.MixedModeOrder > GanjoorPage.Poem.MixedModeOrder).Any())
                        {
                            var nextCat = GanjoorPage.Poem.Category.Cat.Children.Where(c => c.MixedModeOrder == 0 || c.MixedModeOrder > GanjoorPage.Poem.MixedModeOrder).OrderBy(c => c.MixedModeOrder).First();
                            NextUrl = nextCat.FullUrl;
                            NextTitle = nextCat.Title;
                        }
                        else
                        if (GanjoorPage.Poem.Category.Cat.Next != null)
                        {
                            NextUrl = GanjoorPage.Poem.Category.Cat.Next.FullUrl;
                            NextTitle = GanjoorPage.Poem.Category.Cat.Next.Title;
                        }

                        if (GanjoorPage.Poem.Previous != null)
                        {
                            PreviousUrl = GanjoorPage.PoetOrCat.Cat.FullUrl + "/" + GanjoorPage.Poem.Previous.UrlSlug;
                            PreviousTitle = GanjoorPage.Poem.Previous.Title + ": " + GanjoorPage.Poem.Previous.Excerpt;
                        }
                        else
                        if (GanjoorPage.Poem.MixedModeOrder > 0
                            && GanjoorPage.Poem.Category.Cat.Children.Where(c => c.MixedModeOrder != 0 && c.MixedModeOrder < GanjoorPage.Poem.MixedModeOrder).Any())
                        {
                            var prevCat = GanjoorPage.Poem.Category.Cat.Children.Where(c => c.MixedModeOrder != 0 && c.MixedModeOrder < GanjoorPage.Poem.MixedModeOrder).OrderByDescending(c => c.MixedModeOrder).First();
                            PreviousUrl = prevCat.FullUrl;
                            PreviousTitle = prevCat.Title;
                        }
                        else
                        if (GanjoorPage.Poem.Category.Cat.Previous != null)
                        {
                            PreviousUrl = GanjoorPage.Poem.Category.Cat.Previous.FullUrl;
                            PreviousTitle = GanjoorPage.Poem.Category.Cat.Previous.Title;
                        }
                    }
                    break;
                case GanjoorPageType.CatPage:
                    {
                        if (GanjoorPage.PoetOrCat.Cat.Next != null)
                        {
                            NextUrl = GanjoorPage.PoetOrCat.Cat.Next.FullUrl;
                            NextTitle = GanjoorPage.PoetOrCat.Cat.Next.Title;
                        }
                        if (GanjoorPage.PoetOrCat.Cat.Previous != null)
                        {
                            PreviousUrl = GanjoorPage.PoetOrCat.Cat.Previous.FullUrl;
                            PreviousTitle = GanjoorPage.PoetOrCat.Cat.Previous.Title;
                        }
                    }
                    break;
            }
        }

        private string GeneratePagingBarHtml(PaginationMetadata paginationMetadata, string routeStartWithQueryStrings)
        {
            string htmlText = $"<div>{Environment.NewLine}";


            if (paginationMetadata != null && paginationMetadata.totalPages > 1)
            {
                if (paginationMetadata.currentPage > 3)
                {
                    htmlText += $"<a href=\"{routeStartWithQueryStrings.Replace("\"", "%22")}&amp;page=1\"><div class=\"circled-number\">1</div></a>{Environment.NewLine} …";
                }
                for (int i = paginationMetadata.currentPage - 2; i <= (paginationMetadata.currentPage + 2); i++)
                {
                    if (i >= 1 && i <= paginationMetadata.totalPages)
                    {
                        if (i == paginationMetadata.currentPage)
                        {
                            htmlText += $"<div class=\"circled-number-diff\">{i}</div>";
                        }
                        else
                        {
                            htmlText += $"<a href=\"{routeStartWithQueryStrings.Replace("\"", "%22")}&amp;page={i}\"><div class=\"circled-number\">{i}</div></a>{Environment.NewLine}";
                        }
                    }
                }
                if (paginationMetadata.totalPages > (paginationMetadata.currentPage + 2))
                {
                    htmlText += $"… <a href=\"{routeStartWithQueryStrings.Replace("\"", "%22")}&amp;page={paginationMetadata.totalPages}\"><div class=\"circled-number\">{paginationMetadata.totalPages}</div></a>{Environment.NewLine}";
                }
            }

            htmlText += $"</div>{Environment.NewLine}";
            return htmlText;
        }



    public string? LastError { get; set; }

        public bool AggressiveCacheEnabled
        {
            get
            {
                try
                {
                    return bool.Parse(Configuration["AggressiveCacheEnabled"] ?? false.ToString());
                }
                catch
                {
                    return false;
                }
            }
        }


        protected readonly IConfiguration Configuration;
        protected readonly HttpClient _httpClient;
        private readonly IMemoryCache _memoryCache;
        public IndexModel(IConfiguration configuration, HttpClient httpClient, IMemoryCache memoryCache)
        {
            Configuration = configuration;
            _httpClient = httpClient;
            _memoryCache = memoryCache;
        }
    }
}
