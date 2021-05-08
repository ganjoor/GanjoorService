using DNTPersianUtils.Core;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RMuseum.Models.Auth.ViewModel;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Auth.ViewModels;
using RSecurityBackend.Models.Generic;
using System;
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
            string htmlText = "";
            if (!string.IsNullOrEmpty(filterUserId))
            {
                RServiceResult<PublicRAppUser> userInfo = await _appUserService.GetUserInformation(Guid.Parse(filterUserId));

                GanjoorUserPublicProfile profile = new GanjoorUserPublicProfile()
                {
                    Id = (Guid)userInfo.Result.Id,
                    NickName = userInfo.Result.NickName,
                    Bio = userInfo.Result.Bio,
                    Website = userInfo.Result.Website,
                    RImageId = userInfo.Result.RImageId
                };
               

                ViewData["Title"] = $"گنجور &raquo; حاشیه‌های {profile.NickName}";

                GanjoorPage.Title = $"حاشیه‌های {profile.NickName}";

                htmlText += $"<div id=\"profile\">{Environment.NewLine}";

                if (!string.IsNullOrEmpty(profile.Website))
                {
                    htmlText += $"<p><a href=\"{profile.Website}\">{profile.NickName}</a></p>{Environment.NewLine}";
                }
                else
                {
                    htmlText += $"<p>{profile.NickName}</p>{Environment.NewLine}";
                }

                if (!string.IsNullOrEmpty(profile.Bio))
                {
                    htmlText += $"<p>{profile.Bio}</p>{Environment.NewLine}";
                }

                htmlText += $"</div>{Environment.NewLine}";
                htmlText += $"<hr />{Environment.NewLine}";


                
            }

            var commentsRes = await _ganjoorService.GetRecentComments(new PagingParameterModel()
            {
                PageNumber = pageNumber,
                PageSize = 20
            }, 
            string.IsNullOrEmpty(filterUserId) ? Guid.Empty : Guid.Parse(filterUserId), true);

            PaginationMetadata paginationMetadata = commentsRes.Result.PagingMeta;
            GanjoorCommentFullViewModel[] comments = commentsRes.Result.Items;


            foreach (var comment in comments)
            {
                htmlText += $"<blockquote>{comment.HtmlComment}{Environment.NewLine}" +
                    $"<p>{comment.AuthorName} <small>در {comment.CommentDate.ToFriendlyPersianDateTextify()}</small> دربارهٔ <a href=\"{comment.Poem.UrlSlug}\">{comment.Poem.Title}</a>" +
                    $"</blockquote>{Environment.NewLine}<hr />{Environment.NewLine}";
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
