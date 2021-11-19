using DNTPersianUtils.Core;
using GanjooRazor.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor;
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
            string auther = string.IsNullOrEmpty(Request.Query["a"]) ? "0" : Request.Query["a"];

            string url = $"{APIRoot.Url}/api/ganjoor/poems/similar?PageNumber={pageNumber}&PageSize=20&metre={metre}&rhyme={rhyme}&poetId={auther}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                return;
            }

            var poems = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoemCompleteViewModel>>();

            GanjoorPage.Title = "شعرهای ";

            if (auther != "0")
            {
                var poetInfo = Poets.Where(p => p.Id == int.Parse(auther)).SingleOrDefault();
                if(poetInfo != null)
                {
                    GanjoorPage.Title += $"{poetInfo.Nickname} ";
                }
            }

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

        public async Task<ActionResult> OnGetSimilarPoemsPartialAsync(int poemId, int skip, string prosodyMetre, string rhymeLetters, string poemFullUrl)
        {
            string url = $"{APIRoot.Url}/api/ganjoor/poem/{poemId}/related?id={poemId}&skip={skip}&itemsCount=21";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return BadRequest(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
            var relatedPoems = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorCachedRelatedPoem>>();

            return new PartialViewResult()
            {
                ViewName = "_SimiPartialView",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = new _SimiPartialViewModel()
                    {
                        RelatedPoems = relatedPoems.ToArray(),
                        Rhythm = prosodyMetre,
                        RhymeLetters = rhymeLetters,
                        Skip = skip,
                        PoemId = poemId,
                        PoemFullUrl = poemFullUrl
                    }
                }
            };
        }

        public async Task<ActionResult> OnGetSimilarPoemsFromPoetPartialAsync(int poetId, string prosodyMetre, string rhymeLetters, string skipPoemFullUrl1, string skipPoemFullUrl2)
        {
            string url = $"{APIRoot.Url}/api/ganjoor/poems/similar?metre={prosodyMetre}&rhyme={rhymeLetters}&poetId={poetId}";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return BadRequest(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
            List<GanjoorPoemCompleteViewModel> selectedPoems = new List<GanjoorPoemCompleteViewModel>();
            List<int> poetMorePoemsLikeThisCount = new List<int>();
            var poems = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoemCompleteViewModel>>();
            if (poems.Any(p => p.FullUrl == skipPoemFullUrl1))
                poems.Remove(poems.Single(p => p.FullUrl == skipPoemFullUrl1));
            if (poems.Any(p => p.FullUrl == skipPoemFullUrl2))
                poems.Remove(poems.Single(p => p.FullUrl == skipPoemFullUrl2));

            foreach (var poem in poems)
            {
                poem.HtmlText = _GetPoemTextExcerpt(poem.HtmlText);
            }

            return new PartialViewResult()
            {
                ViewName = "_SimiPartialFromPoetView",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = new _SimiPartialFromPoetViewModel()
                    {
                        Poems = poems
                    }
                }
            };
        }
    }
}
