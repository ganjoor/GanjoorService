using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DNTPersianUtils.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Services;
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
        /// ganjoor service
        /// </summary>
        private readonly IGanjoorService _ganjoorService;


        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="memoryCache"></param>
        /// <param name="ganjoorService"></param>
        public SearchModel(IMemoryCache memoryCache, IGanjoorService ganjoorService)
        {
            _memoryCache = memoryCache;
            _ganjoorService = ganjoorService;
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
                var resPoets = await _ganjoorService.GetPoets(true, false);
                if (string.IsNullOrEmpty(resPoets.ExceptionString))
                {
                    poets = new List<GanjoorPoetViewModel>(resPoets.Result);
                    _memoryCache.Set(cacheKey, poets);
                }
            }

            Poets = poets;
        }

        private async Task preparePoet()
        {
            var cacheKey = $"/api/ganjoor/poet/{PoetId}";

            if (!_memoryCache.TryGetValue(cacheKey, out GanjoorPoetCompleteViewModel poet))
            {
                var resPoets = await _ganjoorService.GetPoetById(PoetId);
                if (string.IsNullOrEmpty(resPoets.ExceptionString))
                {
                    poet = resPoets.Result;
                    _memoryCache.Set(cacheKey, poet);
                }
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

            var searchRes = await _ganjoorService.Search
                (
                new PagingParameterModel()
                {
                    PageNumber = pageNumber,
                    PageSize = 20
                },
                Query,
                PoetId == 0 ? null : PoetId,
                CatId == 0 ? null : CatId
                );


            Poems = new List<GanjoorPoemCompleteViewModel>(searchRes.Result.Items);
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

                PagingToolsHtml = GeneratePagingBarHtml(searchRes.Result.PagingMeta, $"/search?s={Query}&author={PoetId}");
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

        private string GeneratePagingBarHtml(PaginationMetadata paginationMetadata, string routeStartWithQueryStrings)
        {
            string htmlText = "<p style=\"text-align: center;\">";

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

            htmlText += $"</p>{Environment.NewLine}";
            return htmlText;
        }
    }

}
