using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor.ViewModels;
using NAudio.Gui;
using RMuseum.Models.Ganjoor;
using System.Net;
using System.Reflection;


namespace TajikGanjoor.Pages
{
    public class IndexModel : PageModel
    {
        public bool IsHomePage { get; set; }
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
        public string HtmlText { get; set; }
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

            IsHomePage = Request.Path == "/";
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
                        HtmlText = PrepareHtmlText(new List<GanjoorVerseViewModel>(GanjoorPage.Poem.Verses));
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
            ViewData["GoogleAnalyticsCode"] = Configuration["GoogleAnalyticsCode"];
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

        private string PrepareHtmlText(List<GanjoorVerseViewModel> verses)
        {
            string htmlText = "";
            int coupletIndex = 0;
            for (int vIndex = 0; vIndex < verses.Count; vIndex++)
            {
                GanjoorVerseViewModel v = verses[vIndex];
                if (v.VersePosition == VersePosition.CenteredVerse1)
                {
                    coupletIndex++;
                    if (((vIndex + 1) < verses.Count) && (verses[vIndex + 1].VersePosition == VersePosition.CenteredVerse2))
                    {
                        htmlText += $"<div class=\"b2\" id=\"bn{coupletIndex}\"><p>{v.Text}</p>{Environment.NewLine}";
                    }
                    else
                    {
                        htmlText += $"<div class=\"b2\" id=\"bn{coupletIndex}\"><p>{v.Text}</p></div>{Environment.NewLine}";

                    }
                }
                else
                if (v.VersePosition == VersePosition.CenteredVerse2)
                {
                    htmlText += $"<p>{v.Text}</p></div>{Environment.NewLine}";
                }
                else

                if (v.VersePosition == VersePosition.Right)
                {
                    coupletIndex++;
                    htmlText += $"<div class=\"b\" id=\"bn{coupletIndex}\"><div class=\"m1\"><p>{v.Text}</p></div>{Environment.NewLine}";
                }
                else
                if (v.VersePosition == VersePosition.Left)
                {
                    htmlText += $"<div class=\"m2\"><p>{v.Text}</p></div></div>{Environment.NewLine}";
                }
                else
                if (v.VersePosition == VersePosition.Comment)
                {
                    htmlText += $"<div class=\"c\"><p>{v.Text}</p></div>{Environment.NewLine}";
                }
                else
                if (v.VersePosition == VersePosition.Paragraph || v.VersePosition == VersePosition.Single)
                {
                    coupletIndex++;
                    string[] lines = v.Text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    string cssClass = v.VersePosition == VersePosition.Paragraph ? "n" : "l";

                    if (lines.Length != 0)
                    {
                        if (v.Text.Length / lines.Length < 150)
                        {
                            htmlText += $"<div class=\"{cssClass}\" id=\"bn{coupletIndex}\"><p>{v.Text.Replace("\r\n", " ")}</p></div>{Environment.NewLine}";
                        }
                        else
                        {
                            foreach (string line in lines)
                                htmlText += $"<div class=\"{cssClass}\" id=\"bn{coupletIndex}\"><p>{line}</p></div>{Environment.NewLine}";
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(v.Text))
                        {
                            htmlText += $"<div class=\"{cssClass}\" id=\"bn{coupletIndex}\"><p>&nbsp;</p></div>{Environment.NewLine}";//empty line!
                        }
                        else
                        {
                            htmlText += $"<div class=\"{cssClass}\" id=\"bn{coupletIndex}\"><p>{v.Text}</p></div>{Environment.NewLine}";//not brave enough to ignore it!
                        }

                    }
                }
            }
            return htmlText.Trim();
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
