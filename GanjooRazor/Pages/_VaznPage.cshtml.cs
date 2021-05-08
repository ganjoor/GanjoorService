using DNTPersianUtils.Core;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace GanjooRazor.Pages
{
    public partial class IndexModel : PageModel
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
                LastError = await response.Content.ReadAsStringAsync();
                return;
            }

            response.EnsureSuccessStatusCode();

            GanjoorPage.Title = "شعرهای ";
            if (poetId != 0)
            {
                GanjoorPage.Title += $"{Poets.Where(p => p.Id == poetId).Single().Name} ";
            }

            GanjoorPage.Title += $"با وزن «{metre}»";




            string htmlText = "";

            if (poetId != 0)
            {
                htmlText += $"<div class=\"sitem\" id=\"all\">{Environment.NewLine}";
                htmlText += $"<h2>{Environment.NewLine}";
                htmlText += $"<a href=\"/vazn/?v={Uri.EscapeUriString(metre)}\">مشاهدهٔ فهرست شعرهای همهٔ شاعران با این وزن</a>{Environment.NewLine}";
                htmlText += $"</h2>{Environment.NewLine}";
                htmlText += $"</div>{Environment.NewLine}";
            }

            var poems = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoemCompleteViewModel>>();





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
            if (!string.IsNullOrEmpty(paginnationMetadata))
            {

                PaginationMetadata paginationMetadata = JsonConvert.DeserializeObject<PaginationMetadata>(paginnationMetadata);

                string queryPoetId = poetId == 0 ? "" : $"&a={poetId}";

                if (paginationMetadata.totalPages > 1)
                {
                    GanjoorPage.Title += $" - صفحهٔ {pageNumber.ToPersianNumbers()}";

                    if (paginationMetadata.currentPage > 3)
                    {
                        htmlText += $"[<a href=\"/vazn/?v={Uri.EscapeUriString(metre)}&page=1{queryPoetId}\">صفحهٔ اول</a>] …";
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
                                htmlText += $"<a href=\"/vazn/?v={Uri.EscapeUriString(metre)}&page={i}{queryPoetId}\">{i.ToPersianNumbers()}</a>";
                            }
                            htmlText += "] ";
                        }
                    }
                    if (paginationMetadata.totalPages > (paginationMetadata.currentPage + 2))
                    {
                        htmlText += $"… [<a href=\"/vazn/?v={Uri.EscapeUriString(metre)}&page={paginationMetadata.totalPages}{queryPoetId}\">صفحهٔ آخر</a>]";
                    }
                }
            }

            htmlText += $"</p>{Environment.NewLine}";

            ViewData["Title"] = $"گنجور » {GanjoorPage.Title}";

            GanjoorPage.HtmlText = htmlText;
        }

        private string _GetPoemTextExcerpt(string poemText)
        {
            poemText = poemText.Replace("<div class=\"b\">", "").Replace("<div class=\"b2\">", "").Replace("<div class=\"m1\">", "").Replace("<div class=\"m2\">", "").Replace("</div>", "");

            int index = poemText.IndexOf("<p>");
            int count = 0;
            while(index != -1 && count < 5 )
            {
                index = poemText.IndexOf("<p>", index + 1);
                count++;
            }

            if(index != -1)
            {
                poemText = poemText.Substring(0, index);
                poemText += "<p>[...]</p>";
            }

            return poemText;
        }
    }
}
