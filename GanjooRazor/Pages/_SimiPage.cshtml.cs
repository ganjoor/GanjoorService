﻿using DNTPersianUtils.Core;
using GanjooRazor.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GanjooRazor.Pages
{
    public partial class IndexModel : PageModel
    {
        /// <summary>
        /// اشعار مشابه
        /// </summary>
        /// <returns></returns>
        private async Task _GenerateSimiHtmlText()
        {
            if (string.IsNullOrEmpty(Request.Query["v"]) || string.IsNullOrEmpty(Request.Query["g"]))
            {
                GanjoorPage.HtmlText = "<p>شعری با مشخصات انتخاب شده یافت نشد.</p>";

            }

            int pageNumber = 1;
            if (!string.IsNullOrEmpty(Request.Query["page"]))
            {
                pageNumber = int.Parse(Request.Query["page"]);
            }

            string metre = Request.Query["v"];
            string rhyme = Request.Query["g"];

            string url = $"{APIRoot.Url}/api/ganjoor/poems/similar?PageNumber={pageNumber}&PageSize=20&metre={metre}&rhyme={rhyme}";
            var response = await _httpClient.GetAsync(url);

            response.EnsureSuccessStatusCode();

            var poems = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoemCompleteViewModel>>();

            GanjoorPage.Title = "شعرهای ";

            GanjoorPage.Title += $"با وزن «{metre}»";

            GanjoorPage.Title += $" و حروف قافیهٔ «{rhyme}»";





            string htmlText = "";




            foreach (var poem in poems)
            {
                htmlText += $"<div class=\"sitem\" id=\"post-{poem.Id}\">{Environment.NewLine}<h2><a href=\"{poem.FullUrl}\" rel=\"bookmark\">{poem.FullTitle}</a>{Environment.NewLine}</h2>{Environment.NewLine}" +
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
                GanjoorPage.Title += $" - صفحهٔ {pageNumber.ToPersianNumbers()}";
                if (paginationMetadata.currentPage > 3)
                {
                    htmlText += $"[<a href=\"/simi/?v={Uri.EscapeUriString(metre)}&g={Uri.EscapeUriString(rhyme)}&page=1\">صفحهٔ اول</a>] …";
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
                            htmlText += $"<a href=\"/simi/?v={Uri.EscapeUriString(metre)}&g={Uri.EscapeUriString(rhyme)}&page={i}\">{i.ToPersianNumbers()}</a>";
                        }
                        htmlText += "] ";
                    }
                }
                if (paginationMetadata.totalPages > (paginationMetadata.currentPage + 2))
                {
                    htmlText += $"… [<a href=\"/simi/?v={Uri.EscapeUriString(metre)}&g={Uri.EscapeUriString(rhyme)}&page={paginationMetadata.totalPages}\">صفحهٔ آخر</a>]";
                }
            }

            htmlText += $"</p>{Environment.NewLine}";

            ViewData["Title"] = $"گنجور » {GanjoorPage.Title}";

            GanjoorPage.HtmlText = htmlText;
        }


        public async Task OnGetSimilarPoemsPartialAsync(int poemId,string prosodyMetre, string rhymeLetters)
        {
            var cacheKey = $"/api/ganjoor/poems/similar/{poemId}";
            if (!_memoryCache.TryGetValue(cacheKey, out InlineSimilarPoems similarPoems))
            {
                string url = $"{APIRoot.Url}/api/ganjoor/poems/similar?PageNumber=1&PageSize=20&metre={prosodyMetre}&rhyme={rhymeLetters}";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    return;
                var poems = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoemCompleteViewModel>>();
                if (poems.Count == 0)
                    return;
                await preparePoets();
                poems.Sort((a, b) => a.Category.Poet.BirthYearInLHijri.CompareTo(b.Category.Poet.BirthYearInLHijri));

                int n = -1;
                int curPoetId = -1;
                List<GanjoorPoemCompleteViewModel> selectedPoems = new List<GanjoorPoemCompleteViewModel>();
                List<int> poetMorePoemsLikeThisCount = new List<int>();
                for (int i = 0; i < poems.Count; i++)
                {

                    var poem = poems[i];
                    if (poem.Id == poemId)
                        continue;
                    if (poem.Category.Poet.Id == curPoetId)
                    {
                        poetMorePoemsLikeThisCount[n]++;
                    }
                    else
                    {
                        n++;
                        if (n >= 5)
                            break;
                        poetMorePoemsLikeThisCount.Add(0);
                        selectedPoems.Add(poem);
                    }
                }
                similarPoems = new InlineSimilarPoems()
                {
                    Poems = selectedPoems,
                    PoetMorePoemsLikeThisCount = poetMorePoemsLikeThisCount
                };
                _memoryCache.Set(cacheKey, similarPoems);
            }
        }
    }
}
