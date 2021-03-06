﻿using DNTPersianUtils.Core;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Auth.ViewModel;
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
        /// صفحهٔ حاشیه‌ها
        /// </summary>
        /// <returns></returns>
        private async Task _GenerateHashiehaHtmlText()
        {
            int pageNumber = 1;
            if (!string.IsNullOrEmpty(Request.Query["page"]))
            {
                pageNumber = int.Parse(Request.Query["page"]);
            }

            
            string filterUserId = Request.Query["userid"];
            string url = $"{APIRoot.Url}/api/ganjoor/comments?PageNumber={pageNumber}&PageSize=20";
            string htmlText = "";
            if (!string.IsNullOrEmpty(filterUserId))
            {
                var responseUserProfile = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/user/profile/{filterUserId}");
                responseUserProfile.EnsureSuccessStatusCode();

                GanjoorUserPublicProfile profile = JsonConvert.DeserializeObject<GanjoorUserPublicProfile>(await responseUserProfile.Content.ReadAsStringAsync());


                ViewData["Title"] = $"گنجور &raquo; حاشیه‌های {profile.NickName}";

                GanjoorPage.Title = $"حاشیه‌های {profile.NickName}";

                htmlText += $"<div id=\"profile\">{Environment.NewLine}";

                htmlText += $"<p>{profile.NickName}{Environment.NewLine}";

                if (!string.IsNullOrEmpty(profile.Website))
                {
                    htmlText += $"<a href=\"{profile.Website}\">🌐</a></p>{Environment.NewLine}";
                }
                
                htmlText += $"</p>{Environment.NewLine}";

                if (!string.IsNullOrEmpty(profile.Bio))
                {
                    htmlText += $"<p>{profile.Bio}</p>{Environment.NewLine}";
                }

                htmlText += $"</div>{Environment.NewLine}";
                htmlText += $"<hr />{Environment.NewLine}";

                url += $"&filterUserId={filterUserId}";

            }

            var response = await _httpClient.GetAsync(url);


            response.EnsureSuccessStatusCode();

            string paginnationMetadata = response.Headers.GetValues("paging-headers").FirstOrDefault();
            PaginationMetadata paginationMetadata = JsonConvert.DeserializeObject<PaginationMetadata>(paginnationMetadata);
            var comments = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorCommentFullViewModel>>();


            foreach (var comment in comments)
            {
                string commentAuthorLink = comment.UserId == null ? comment.AuthorName : $"<a href=\"/hashieha/?userid={comment.UserId}\">{comment.AuthorName}</a>";
                string inReplyTo = comment.InReplayTo == null ? "" : $" در پاسخ به {comment.InReplayTo.AuthorName} ";
                htmlText += $"<p>{commentAuthorLink} <small>در {comment.CommentDate.ToFriendlyPersianDateTextify()}{inReplyTo}</small> دربارهٔ <a href=\"{comment.Poem.UrlSlug}#comment-{comment.Id}\">{comment.Poem.Title}</a>:</p>" + 
                    $"<blockquote>{comment.HtmlComment}{Environment.NewLine}" +
                    $"</blockquote>{Environment.NewLine}<div class='spacer'>&nbsp;</div><hr />{Environment.NewLine}";
            }

            htmlText += "<p style=\"text-align: center;\">";

            string queryFilterUserId = string.IsNullOrEmpty(filterUserId) ? "" : $"&userid={filterUserId}";

            if (paginationMetadata.totalPages > 1)
            {
                GanjoorPage.Title += $" - صفحهٔ {pageNumber.ToPersianNumbers()}";
                ViewData["Title"] += $" - صفحهٔ {pageNumber.ToPersianNumbers()}";

                if (paginationMetadata.currentPage > 3)
                {
                    htmlText += $"[<a href=\"/hashieha/?page=1{queryFilterUserId}\">صفحهٔ اول</a>] …";
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
                            htmlText += $"<a href=\"/hashieha/?page={i}{queryFilterUserId}\">{i.ToPersianNumbers()}</a>";
                        }
                        htmlText += "] ";
                    }
                }
                if (paginationMetadata.totalPages > (paginationMetadata.currentPage + 2))
                {
                    htmlText += $"… [<a href=\"/hashieha/?page={paginationMetadata.totalPages}{queryFilterUserId}\">صفحهٔ آخر</a>]";
                }
            }

            htmlText += $"</p>{Environment.NewLine}";

            GanjoorPage.HtmlText = htmlText;


        }
    }
}
