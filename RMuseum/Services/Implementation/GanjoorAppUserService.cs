using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Auth.Db;
using RSecurityBackend.Models.Auth.ViewModels;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services;
using RSecurityBackend.Services.Implementation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RMuseum.Services.Implementation
{
    /// <summary>
    /// Ganjoor User Service
    /// </summary>
    public class GanjoorAppUserService : AppUserService
    {
        public GanjoorAppUserService(
            RMuseumDbContext context,
            UserManager<RAppUser> userManager,
            SignInManager<RAppUser> signInManager,
            RoleManager<RAppRole> roleManager,
            ISecretGenerator secretGenerator,
            IImageFileService imageFileService,
            IUserRoleService userRoleService,
            IConfiguration configuration,
            IMemoryCache memoryCache
            )
            : base(context, userManager, signInManager, roleManager, secretGenerator, imageFileService, userRoleService, configuration)
        {
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// IMemoryCache
        /// </summary>
        protected readonly IMemoryCache _memoryCache;

        /// <summary>
        /// Ganjoor Service
        /// </summary>
        protected IGanjoorService _ganjoorService { get; set; }


        /// <summary>
        /// Sign Up Email Subject
        /// </summary>
        /// <returns>
        /// subject
        /// </returns>
        /// <param name="op"></param>
        /// <param name="secretCode"></param>
        public override string GetEmailSubject(RVerifyQueueType op, string secretCode)
        {
            string opString = 
                op == RVerifyQueueType.SignUp ? "ثبت نام"
                :
                op == RVerifyQueueType.ForgotPassword
                ?
                "بازیابی کلمهٔ عبور"
                :
                "حذف حساب کاربری";
            return $"{secretCode} کد {opString} شما در گنجور";
        }

        /// <summary>
        /// Sign Up Email Html Content
        /// </summary>
        /// <param name="op"></param>
        /// <param name="secretCode"></param>
        /// <param name="signupCallbackUrl"></param>
        /// <returns>html content</returns>
        public override string GetEmailHtmlContent(RVerifyQueueType op, string secretCode, string signupCallbackUrl)
        {
            string opString = op == RVerifyQueueType.SignUp ? "ثبت نام" : op == RVerifyQueueType.ForgotPassword ? "بازیابی کلمهٔ عبور" : "حذف حساب کاربری";
            string ifNot = op == RVerifyQueueType.SignUp ? "اگر در گنجور ثبت نام نکرده‌اید لطفاً این نامه را نادیده بگیرید."
                                : op == RVerifyQueueType.ForgotPassword ?
                                "اگر در گنجور فراموشی گذرواژه را نزده‌اید یا گذرواژه‌تان را به خاطر آوردید لطفاً این نامه را نادیده بگیرید."
                                :
                                op == RVerifyQueueType.UserSelfDelete ? 
                                "اگر در گنجور حذف حساب کاربری را نزده‌اید یا از حذف حساب کاربریتان منصرف شده‌اید لطفاً این نامه را نادیده بگیرید."
                                :
                                "";
            string content =
               "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">"
               +
               "<html xmlns=\"http://www.w3.org/1999/xhtml\" xml:lang=\"en\" lang=\"fa\">"
               +
               "<head>"
               +
               "<meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\" />"
               +
               (op == RVerifyQueueType.KickOutUser ? $"<title>حذف حساب کاربری شما در گنجور</title>" : $" <title>کد {opString} شما در گنجور: {secretCode}</title>")
               +
               "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"/>"
               +
               "</head>"
                +
                "<body style=\"font:normal 12px tahoma;direction:rtl\">"
                +
                "<table align=\"center\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" width=\"600\">"
                +
                "<tr>"
                +
                "<td align=\"center\" style=\"padding: 40px 0 30px 0;\">"
                +
                "<img src=\"https://i.ganjoor.net/gm.gif\" alt=\"گنجور\" width=\"150\" height=\"150\" style=\"display: block;\" />"
                +
                "</td>"
                +
                "</tr>"
                +
                "<tr><td>"
                +
                (
                op == RVerifyQueueType.KickOutUser ?
                $"<p style=\"font:normal 12px tahoma;direction:rtl\">کاربر گرامی، متأسفیم که به اطلاع برسانیم که به دلیل نقض قوانین استفاده از گنجور و به طور مشخص {secretCode} حساب کاربری شما به همراه حاشیه‌ها، خوانش‌ها و سایر اطلاعات خصوصیتان از گنجور حذف شده است. امیدواریم در آینده در صورت تمایل به استفاده از گنجور در چارچوب‌های قابل پذیرش برای ما با حساب کاربری جدیدی پذیرای شما باشیم. با این ایمیل امکان ثبت نام مجدد نخواهید داشت.</p>"
                :
                string.IsNullOrEmpty(signupCallbackUrl) ?
                $"<p style=\"font:normal 12px tahoma;direction:rtl\">لطفاً جهت تکمیل {opString} در گنجور کد <strong>{secretCode}</strong> را به عنوان رمز دریافتی در صفحهٔ {opString} وارد کنید.</p>"
                :
                $"<p style=\"font:normal 12px tahoma;direction:rtl\">لطفاً جهت تکمیل {opString} در گنجور <a href=\"{signupCallbackUrl}?secret={secretCode}\">اینجا</a> کلیک کنید یا اگر صفحهٔ {opString} هنوز باز است کد <strong>{secretCode}</strong> را در آن وارد کنید.</p>"
                )
                +
                "</td></tr>"
                +
                "<tr><td>"
                +
                $"<p style=\"font:normal 12px tahoma;direction:rtl\">{ifNot} به این نامه پاسخ ندهید، کسی پاسخگوی شما نخواهد بود.</p>"
                +
                "</td></tr>"
                +
                "</table>"
                +
                "</body>"
                +
               "</html>"
               ;

            return content;
        }

        /// <summary>
        /// finalize signup and assign his or him comments to him or her
        /// </summary>
        /// <param name="email"></param>
        /// <param name="secret"></param>
        /// <param name="password"></param>
        /// <param name="firstName"></param>
        /// <param name="sureName"></param>
        /// <returns></returns>
        public override async Task<RServiceResult<bool>> FinalizeSignUp(string email, string secret, string password, string firstName, string sureName)
        {
            RServiceResult<bool> res = await base.FinalizeSignUp(email, secret, password, firstName, sureName);
            if(res.Result)
            {
                try
                {
                    RMuseumDbContext context = _context as RMuseumDbContext;
                    var user = (await FindUserByEmail(email)).Result;
                    if(user.EmailConfirmed)
                    {
                        var comments = await context.GanjoorComments.Where(c => c.AuthorEmail == email.ToLower()).ToListAsync();
                        if (comments.Count > 0)
                        {

                            foreach (var comment in comments)
                            {
                                comment.UserId = user.Id;
                            }
                            _context.UpdateRange(comments);
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                catch
                {
                    return new RServiceResult<bool>(true); //ignore this error! because signup was succesfull
                }
            }
            return res;
        }

        /// <summary>
        /// modify existing user /*update related entities cache*/
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="updateUserInfo"></param>
        /// <returns></returns>
        public override async Task<RServiceResult<bool>> ModifyUser(Guid userId, RegisterRAppUser updateUserInfo)
        {
            try
            {
                RAppUser unmodifiedUserInfo = await _userManager.FindByIdAsync(userId.ToString());
                if (unmodifiedUserInfo == null)
                {
                    return new RServiceResult<bool>(false, "کاربر مورد نظر یافت نشد");
                }

                string nickName = updateUserInfo.NickName;

                if (string.IsNullOrEmpty(nickName))
                {
                    return new RServiceResult<bool>(false, "نام مستعار نمی‌تواند خالی باشد.");
                }

                nickName = nickName.Trim();

                RServiceResult<bool> res = await base.ModifyUser(userId, updateUserInfo);
                if (res.Result)
                {
                    try
                    {
                        if (nickName != updateUserInfo.NickName)
                        {
                            RMuseumDbContext context = _context as RMuseumDbContext;
                            var poemIdSet = await context.GanjoorComments.AsNoTracking().Where(c => c.UserId == userId).Select(c => c.PoemId).ToListAsync();
                            foreach (var poemId in poemIdSet)
                            {
                                //await _ganjoorService.CacheCleanForPageById(poemId); /*had error in service initializtion, so done it in the dirty way*/

                                var dbPage = await context.GanjoorPages.Where(p => p.Id == poemId).AsNoTracking().SingleOrDefaultAsync();
                                if (dbPage != null)
                                {
                                    //CacheCleanForPageByUrl(dbPage.FullUrl);
                                    var url = dbPage.FullUrl;
                                    var cachKey = $"GanjoorService::GetPageByUrl::{url}";
                                    if (_memoryCache.TryGetValue(cachKey, out GanjoorPageCompleteViewModel page))
                                    {
                                        _memoryCache.Remove(cachKey);

                                        var poemCachKey = $"GetPoemById({page.Id}, {true}, {false}, {true}, {true}, {true}, {true}, {true}, {true}, {true})";
                                        if (_memoryCache.TryGetValue(poemCachKey, out GanjoorPoemCompleteViewModel p))
                                        {
                                            _memoryCache.Remove(poemCachKey);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        return new RServiceResult<bool>(true); //ignore this error! because main operation was successfull!
                    }

                }
                return res;
            }
            catch(Exception exp)
            {
                return new RServiceResult<bool>(false, exp.ToString());
            }

        }


        /// <summary>
        /// remove user data
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public override async Task<RServiceResult<bool>> RemoveUserData(Guid userId)
        {
            RMuseumDbContext context = _context as RMuseumDbContext;

            string systemEmail = $"{Configuration.GetSection("Ganjoor")["SystemEmail"]}";
            var systemUserId = (Guid)(await FindUserByEmail(systemEmail)).Result.Id;

            if(systemUserId == userId)
            {
                return new RServiceResult<bool>(false, "تلاش برای حذف کاربر سیستمی");
            }

            var reviewedRecitations = await context.Recitations.Where(r => r.ReviewerId == userId).ToListAsync();
            foreach(var reviewedRecitation in reviewedRecitations)
                reviewedRecitation.ReviewerId = systemUserId;
            context.UpdateRange(reviewedRecitations);

            var suggestedCorrections = await context.GanjoorPoemCorrections.Where(c => c.UserId == userId).ToListAsync();
            foreach (var suggestedCorrection in suggestedCorrections)
                suggestedCorrection.UserId = systemUserId;
            context.UpdateRange(suggestedCorrections);

            var reviewedCorrections = await context.GanjoorPoemCorrections.Where(c => c.ReviewerUserId == userId).ToListAsync();
            foreach (var reviewedCorrection in reviewedCorrections)
                reviewedCorrection.UserId = systemUserId;
            context.UpdateRange(reviewedCorrections);

            var reportedComments = await context.GanjoorReportedComments.Where(r => r.ReportedById == userId).ToListAsync();
            foreach (var reportedComment in reportedComments)
                reportedComment.ReportedById = systemUserId;
            context.UpdateRange(reportedComments);

            var ganjoorLinks = await context.GanjoorLinks.Where(l => l.SuggestedById == userId).ToListAsync();
            foreach (var ganjoorLink in ganjoorLinks)
                ganjoorLink.SuggestedById = systemUserId;
            context.UpdateRange(ganjoorLinks);

            var reviewedGanjoorLinks = await context.GanjoorLinks.Where(l => l.ReviewerId == userId).ToListAsync();
            foreach (var reviewedGanjoorLink in reviewedGanjoorLinks)
                reviewedGanjoorLink.ReviewerId = systemUserId;
            context.UpdateRange(reviewedGanjoorLinks);

            var pinLinks = await context.PinterestLinks.Where(l => l.SuggestedById == userId).ToListAsync();
            foreach (var pinLink in pinLinks)
                pinLink.SuggestedById = systemUserId;
            context.UpdateRange(pinLinks);

            var reviewedPinLinks = await context.GanjoorLinks.Where(l => l.ReviewerId == userId).ToListAsync();
            foreach (var reviewedPinLink in reviewedPinLinks)
                reviewedPinLink.ReviewerId = systemUserId;
            context.UpdateRange(reviewedPinLinks);

            var poemMusicTracks = await context.GanjoorPoemMusicTracks.Where(m => m.SuggestedById == userId).ToListAsync();
            foreach (var poemMusicTrack in poemMusicTracks)
                poemMusicTrack.SuggestedById = systemUserId;
            context.UpdateRange(poemMusicTracks);

            var snapshots = await context.GanjoorPageSnapshots.Where(s => s.MadeObsoleteByUserId == userId).ToListAsync();
            foreach (var snapshot in snapshots)
                snapshot.MadeObsoleteByUserId = systemUserId;
            context.UpdateRange(snapshots);

            await context.SaveChangesAsync();

            List<int> poemsNeededToBeRefreshed = new List<int>();

            var bookmarks = await context.UserBookmarks.Where(b => b.RAppUserId == userId).ToListAsync();
            context.RemoveRange(bookmarks);

            var uploadSessions = await context.UploadSessions.Where(s => s.UseId == userId).ToListAsync();
            context.RemoveRange(uploadSessions);

            var recitations = await context.Recitations.Where(r => r.OwnerId == userId).ToListAsync();
            poemsNeededToBeRefreshed.AddRange(recitations.Select(r => r.GanjoorPostId).ToList());
            context.RemoveRange(recitations);

            await context.SaveChangesAsync();

            var comments = await context.GanjoorComments.Where(c => c.UserId == userId).ToListAsync();
            poemsNeededToBeRefreshed.AddRange(comments.Select(c => c.PoemId).ToList());
            foreach (var comment in comments)
            {
                //await _ganjoorService.DeleteMyComment(userId, comment.Id);/*had error in service initializtion, so done it in the dirty way*/
                await DeleteComment(context, userId, comment.Id);
            }


            return await base.RemoveUserData(userId);//notifications are deleted here, some of these operations might produce new notifications
        }

        private async Task<RServiceResult<bool>> DeleteComment(RMuseumDbContext context, Guid userId, int commentId)
        {
            GanjoorComment comment = await context.GanjoorComments.Where(c => c.Id == commentId && c.UserId == userId).SingleOrDefaultAsync();//userId is not part of key but it helps making call secure
            if (comment == null)
            {
                return new RServiceResult<bool>(false); //not found
            }

            //if user has got replies, delete them and notify their owners of what happened
            var replies = await _FindReplies(context, comment);
            for (int i = replies.Count - 1; i >= 0; i--)
            {
                context.GanjoorComments.Remove(replies[i]);
            }

            context.GanjoorComments.Remove(comment);
            await _context.SaveChangesAsync();

            return new RServiceResult<bool>(true);
        }

        private async Task<List<GanjoorComment>> _FindReplies(RMuseumDbContext context, GanjoorComment comment)
        {
            List<GanjoorComment> replies = await context.GanjoorComments.Where(c => c.InReplyToId == comment.Id).AsNoTracking().ToListAsync();
            List<GanjoorComment> replyToReplies = new List<GanjoorComment>();
            foreach (GanjoorComment reply in replies)
            {
                replyToReplies.AddRange(await _FindReplies(context, reply));
            }
            if (replyToReplies.Count > 0)
            {
                replies.AddRange(replyToReplies);
            }
            return replies;
        }
    }
}
