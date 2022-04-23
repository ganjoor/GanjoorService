using DNTPersianUtils.Core;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Utils;
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
        /// صفحهٔ اشعار با وزن خاص
        /// </summary>
        /// <returns></returns>
        private async Task _GenerateVaznHtmlText()
        {
            if (string.IsNullOrEmpty(Request.Query["v"]))
                return;
            int pageNumber = 1;
            if (!string.IsNullOrEmpty(Request.Query["page"]))
            {
                pageNumber = int.Parse(Request.Query["page"]);
            }

            string metre = Request.Query["v"];
            int poetId = string.IsNullOrEmpty(Request.Query["a"]) ? 0 : int.Parse(Request.Query["a"]);

            string url = $"{APIRoot.Url}/api/ganjoor/poems/similar?PageNumber={pageNumber}&PageSize=20&metre={metre}&poetId={poetId}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                return;
            }

            GanjoorPage.Title = "شعرهای ";
            if (poetId != 0)
            {
                var poetInfo = Poets.Where(p => p.Id == poetId).SingleOrDefault();
                if(poetInfo != null)
                {
                    GanjoorPage.Title += $"{poetInfo.Nickname} ";
                }
            }

            GanjoorPage.Title += $"با وزن «{metre}»";




            string htmlText = "";

            htmlText += $"<div class=\"sitem\" id=\"all\">{Environment.NewLine}";
            htmlText += $"<h2>{GanjoorPage.Title}</h2>";
            if (poetId != 0)
            {
                htmlText += $"<p>{Environment.NewLine}";
                htmlText += $"<a href=\"/vazn/?v={Uri.EscapeDataString(metre)}\">مشاهدهٔ فهرست شعرهای همهٔ شاعران با این وزن</a>{Environment.NewLine}";
                htmlText += $"</p>{Environment.NewLine}";
            }
            htmlText += $"</div>{Environment.NewLine}";

            var poems = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoemCompleteViewModel>>();

            foreach (var poem in poems)
            {
                htmlText += $"<div class=\"sitem\" id=\"post-{poem.Id}\">{Environment.NewLine}<h2><a href=\"{poem.FullUrl}\" rel=\"bookmark\">{poem.FullTitle}</a>{Environment.NewLine}</h2>{Environment.NewLine}" +
                    $"<div class=\"spacer\">&nbsp;</div>{Environment.NewLine}"
                    +
                    $"<div class=\"sit\">{Environment.NewLine} {GanjoorPoemTools.GetPoemHtmlExcerpt(poem.HtmlText)}" +
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

            string queryPoetId = poetId == 0 ? "" : $"&a={poetId}";

            if (paginationMetadata.totalPages > 1)
            {
                GanjoorPage.Title += $" - صفحهٔ {pageNumber.ToPersianNumbers()}";

                if (paginationMetadata.currentPage > 3)
                {
                    htmlText += $"[<a href=\"/vazn/?v={Uri.EscapeDataString(metre)}&page=1{queryPoetId}\">صفحهٔ اول</a>] …";
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
                            htmlText += $"<a href=\"/vazn/?v={Uri.EscapeDataString(metre)}&page={i}{queryPoetId}\">{i.ToPersianNumbers()}</a>";
                        }
                        htmlText += "] ";
                    }
                }
                if (paginationMetadata.totalPages > (paginationMetadata.currentPage + 2))
                {
                    htmlText += $"… [<a href=\"/vazn/?v={Uri.EscapeDataString(metre)}&page={paginationMetadata.totalPages}{queryPoetId}\">صفحهٔ آخر</a>]";
                }
            }

            htmlText += $"</p>{Environment.NewLine}";

            ViewData["Title"] = $"گنجور » {GanjoorPage.Title}";

            GanjoorPage.HtmlText = htmlText;
        }
    }
}
