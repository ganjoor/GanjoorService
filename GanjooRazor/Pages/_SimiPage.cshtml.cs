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
    public partial class IndexModel : LoginPartialEnabledPageModel
    {
        /// <summary>
        /// اشعار مشابه
        /// </summary>
        /// <returns></returns>
        private async Task _GenerateSimiHtmlText()
        {
            if (string.IsNullOrEmpty(Request.Query["v"]) || string.IsNullOrEmpty(Request.Query["g"]))
            {
                GanjoorPage.HtmlText = "<p>موردی با مشخصات انتخاب شده یافت نشد.</p>";
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

            GanjoorPage.Title = "شعرها یا ابیات ";

            if (author != "0")
            {
                var poetInfo = Poets.Where(p => p.Id == int.Parse(author)).SingleOrDefault();
                if(poetInfo != null)
                {
                    GanjoorPage.Title += $"{poetInfo.Nickname} ";
                }
            }
            GanjoorPage.Title += $"با وزن «{metre}»";
            GanjoorPage.Title += $" و حروف قافیهٔ «{rhyme}»";

            string htmlText = "";

            htmlText += $"<div class=\"sitem\" id=\"all\">{Environment.NewLine}";
            htmlText += $"<h2>{GanjoorPage.Title}</h2>";
            htmlText += $"</div>{Environment.NewLine}";


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
                string authorParam = author != "0" ? $"&amp;a={author}" : "";
                GanjoorPage.Title += $" - صفحهٔ {pageNumber.ToPersianNumbers()}";
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

            ViewData["Title"] = $"گنجور » {GanjoorPage.Title}";

            GanjoorPage.HtmlText = htmlText;
        }

        public async Task<ActionResult> OnGetSimilarPoemsPartialAsync(int poemId, int skip, string prosodyMetre, string rhymeLetters, string poemFullUrl, int sectionIndex)
        {
            string url = $"{APIRoot.Url}/api/ganjoor/section/{poemId}/{sectionIndex}/related?skip={skip}&itemsCount=21";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return BadRequest(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
            var relatedSections = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorCachedRelatedSection>>();

            return new PartialViewResult()
            {
                ViewName = "_SimiPartialView",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = new _SimiPartialViewModel()
                    {
                        RelatedSections = relatedSections.ToArray(),
                        Rhythm = prosodyMetre,
                        RhymeLetters = rhymeLetters,
                        Skip = skip,
                        PoemId = poemId,
                        PoemFullUrl = poemFullUrl,
                        SectionIndex = sectionIndex
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
                poems.RemoveAll(p => p.FullUrl == skipPoemFullUrl1); //TODO: fix errors here
            if (poems.Any(p => p.FullUrl == skipPoemFullUrl2))
                poems.RemoveAll(p => p.FullUrl == skipPoemFullUrl2);

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
                        Poems = poems,
                    }
                }
            };
        }
    }
}
