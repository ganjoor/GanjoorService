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

        public string Query { get; set; }
        public int PoetId { get; set; }
        public string Metre { get; set; }
        public string Rhyme { get; set; }
        public int CatId { get; set; }
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

            Query = Request.Query["s"].ApplyCorrectYeKe().Trim();
            bool quotes = Query.IndexOf("\"") != -1;
            Query = LanguageUtils.MakeTextSearchable(Query); //replace zwnj with space
            if (quotes)
                Query = $"\"{Query}\"";
            PoetId = string.IsNullOrEmpty(Request.Query["a"]) ? 0 : int.Parse(Request.Query["a"]);

            ViewData["GoogleAnalyticsCode"] = Configuration["GoogleAnalyticsCode"];

            //todo: use html master layout or make it partial
            // 1. poets 
            if (false == (await preparePoets()))
                return Page();

            var poetName = Poets.SingleOrDefault(p => p.Id == PoetId);
            if (poetName != null)
            {
                ViewData["Title"] = $"گنجور » نتایج جستجو برای {Query} در آثار {poetName?.Name}";
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
                poem.HtmlText = _GetPoemTextExcerpt(poem.HtmlText);
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
        /// <summary>
        /// اشعار مشابه
        /// </summary>
        /// <returns></returns>
        private async Task _GenerateSimiHtmlText()
        {
            if (string.IsNullOrEmpty(Request.Query["v"]) || string.IsNullOrEmpty(Request.Query["g"]))
            {
                LastError = "<p>موردی با مشخصات انتخاب شده یافت نشد.</p>";
                return;
            }

            int pageNumber = 1;
            if (!string.IsNullOrEmpty(Request.Query["page"]))
            {
                pageNumber = int.Parse(Request.Query["page"]);
            }

            string metre = Request.Query["v"];
            string rhyme = Request.Query["g"];

            string author = string.IsNullOrEmpty(Request.Query["a"]) ? "0" : Request.Query["a"];

            string url = $"{APIRoot.Url}/api/ganjoor/poems/similar?PageNumber={pageNumber}&PageSize=20&metre={metre}&rhyme={rhyme}&poetId={author}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                return;
            }

            var poems = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoemCompleteViewModel>>();

            string title = "شعرها یا ابیات ";

            if (author != "0")
            {
                var poetInfo = Poets.Where(p => p.Id == int.Parse(author)).SingleOrDefault();
                if (poetInfo != null)
                {
                    title += $"{poetInfo.Nickname} ";
                }
            }
            title += $"با وزن «{metre}»";
            title += $" و حروف قافیهٔ «{rhyme}»";

            string htmlText = "";

            htmlText += $"<div class=\"sitem\" id=\"all\">{Environment.NewLine}";
            htmlText += $"<h2>{title}</h2>";
            htmlText += $"</div>{Environment.NewLine}";


            foreach (var poem in poems)
            {
                htmlText += $"<div class=\"sitem\">{Environment.NewLine}<h2><a href=\"{poem.FullUrl}\" rel=\"bookmark\">{poem.FullTitle}</a>{Environment.NewLine}</h2>{Environment.NewLine}" +
                    $"<div class=\"spacer\">&nbsp;</div>{Environment.NewLine}"
                    +
                    $"<div class=\"sit\">{Environment.NewLine} {_GetPoemTextExcerpt(poem.HtmlText)}" +
                    $"<p><br /><a href=\"{poem.FullUrl}\">متن کامل شعر را ببینید ...</a></p>" +
                    $"</div>{Environment.NewLine}" +
                    $"<img src=\"{APIRoot.InternetUrl}{poem.Category.Poet.ImageUrl}\" alt=\"{poem.Category.Poet.Name}\" />"
                    +
                    $"<div class=\"spacer\">&nbsp;</div>{Environment.NewLine}"
                    +
                    $"</div>{Environment.NewLine}";
            }

            htmlText += "<p style=\"text-align: center;\">";

            string paginnationMetadata = response.Headers.GetValues("paging-headers").FirstOrDefault();

            PaginationMetadata paginationMetadata = JsonConvert.DeserializeObject<PaginationMetadata>(paginnationMetadata);

            if (paginationMetadata.totalPages > 1)
            {
                string authorParam = author != "0" ? $"&amp;a={author}" : "";
                title += $" - صفحهٔ {pageNumber.ToPersianNumbers()}";
                if (paginationMetadata.currentPage > 3)
                {
                    htmlText += $"[<a href=\"/simi/?v={Uri.EscapeDataString(metre)}&amp;g={Uri.EscapeDataString(rhyme)}&amp;page=1{authorParam}\">صفحهٔ اول</a>] …";
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
                            htmlText += $"<a href=\"/simi/?v={Uri.EscapeDataString(metre)}&amp;g={Uri.EscapeDataString(rhyme)}&amp;page={i}{authorParam}\">{i.ToPersianNumbers()}</a>";
                        }
                        htmlText += "] ";
                    }
                }
                if (paginationMetadata.totalPages > (paginationMetadata.currentPage + 2))
                {
                    htmlText += $"… [<a href=\"/simi/?v={Uri.EscapeDataString(metre)}&amp;g={Uri.EscapeDataString(rhyme)}&amp;page={paginationMetadata.totalPages}{authorParam}\">صفحهٔ آخر</a>]";
                }
            }

            htmlText += $"</p>{Environment.NewLine}";

            ViewData["Title"] = $"گنجور » {title}";

            HtmlText = htmlText;
        }

        private string _GetPoemTextExcerpt(string poemText)
        {
            while (poemText.IndexOf("id=\"bn") != -1)
            {
                int idxbn1 = poemText.IndexOf(" id=\"bn");
                int idxbn2 = poemText.IndexOf("\"", idxbn1 + " id=\"bn".Length);
                poemText = poemText.Substring(0, idxbn1) + poemText.Substring(idxbn2 + 1);
            }

            poemText = poemText.Replace("<div class=\"b\">", "").Replace("<div class=\"b2\">", "").Replace("<div class=\"m1\">", "").Replace("<div class=\"m2\">", "").Replace("</div>", "");

            int index = poemText.IndexOf("<p>");
            int count = 0;
            while (index != -1 && count < 5)
            {
                index = poemText.IndexOf("<p>", index + 1);
                count++;
            }

            if (index != -1)
            {
                poemText = poemText.Substring(0, index);
                poemText += "<p>[...]</p>";
            }

            return poemText;
        }
    }
}
