using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Models.GanjoorAudio;
using RMuseum.Models.GanjoorAudio.ViewModels;
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
                op == RVerifyQueueType.SignUp ? "نام‌نویسی"
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
            string opString = op == RVerifyQueueType.SignUp ? "نام‌نویسی" : op == RVerifyQueueType.ForgotPassword ? "بازیابی کلمهٔ عبور" : "حذف حساب کاربری";
            string ifNot = op == RVerifyQueueType.SignUp ? "اگر در گنجور نام‌نویسی نکرده‌اید لطفاً این نامه را نادیده بگیرید."
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
                $"<p style=\"font:normal 12px tahoma;direction:rtl\">کاربر گرامی، متأسفیم که به اطلاع برسانیم که به دلیل نقض قوانین استفاده از گنجور و به طور مشخص {secretCode} حساب کاربری شما به همراه حاشیه‌ها، خوانش‌ها و سایر اطلاعات خصوصیتان از گنجور حذف شده است. امیدواریم در آینده در صورت تمایل به استفاده از گنجور در چارچوب‌های قابل پذیرش برای ما با حساب کاربری جدیدی پذیرای شما باشیم. با این ایمیل امکان نام‌نویسی مجدد نخواهید داشت.</p>"
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
        /// Start signup process using email
        /// </summary>
        /// <param name="email"></param>
        /// <param name="clientIPAddress"></param>
        /// <param name="clientAppName"></param>
        /// <param name="langauge"></param>
        /// <returns></returns>
        public override async Task<RServiceResult<RVerifyQueueItem>> SignUp(string email, string clientIPAddress, string clientAppName, string langauge)
        {
            RServiceResult<RVerifyQueueItem> res = await base.SignUp(email, clientIPAddress, clientAppName, langauge);
            if(res.Result == null)
            {
                if(res.ExceptionString == "شما قبلا نام‌نویسی کرده‌اید.")
                {
                    return new RServiceResult<RVerifyQueueItem>(null, "شما قبلا نام‌نویسی کرده‌اید. توجه بفرمایید که کاربران گنجینهٔ گنجور و پیشخان خوانشگران یکسانند و می‌توانید با همان نام کاربری اینجا وارد شوید.");
                }
            }
            return res;
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

            string deletedUserEmail = $"{Configuration.GetSection("Ganjoor")["DeleteUserEmail"]}";
            var deletedUserId = (Guid)(await FindUserByEmail(deletedUserEmail)).Result.Id;

            if (deletedUserId == userId)
            {
                return new RServiceResult<bool>(false, "تلاش برای حذف کاربر سیستمی کاربر حذف شده");
            }

            var reviewedRecitations = await context.Recitations.Where(r => r.ReviewerId == userId).ToListAsync();
            foreach(var reviewedRecitation in reviewedRecitations)
                reviewedRecitation.ReviewerId = deletedUserId;
            context.UpdateRange(reviewedRecitations);
            await context.SaveChangesAsync(); //some tracking data related bugs makes it necessary to call this especially for same table data processing

            var suggestedCorrections = await context.GanjoorPoemCorrections.Where(c => c.UserId == userId).ToListAsync();
            foreach (var suggestedCorrection in suggestedCorrections)
                suggestedCorrection.UserId = deletedUserId;
            context.UpdateRange(suggestedCorrections);
            await context.SaveChangesAsync();

            var reviewedCorrections = await context.GanjoorPoemCorrections.Where(c => c.ReviewerUserId == userId).ToListAsync();
            foreach (var reviewedCorrection in reviewedCorrections)
                reviewedCorrection.ReviewerUserId = deletedUserId;
            context.UpdateRange(reviewedCorrections);
            await context.SaveChangesAsync();

            var suggestedSectionCorrections = await context.GanjoorPoemSectionCorrections.Where(c => c.UserId == userId).ToListAsync();
            foreach (var suggestedCorrection in suggestedSectionCorrections)
                suggestedCorrection.UserId = deletedUserId;
            context.UpdateRange(suggestedSectionCorrections);
            await context.SaveChangesAsync();

            var reviewedSectionCorrections = await context.GanjoorPoemSectionCorrections.Where(c => c.ReviewerUserId == userId).ToListAsync();
            foreach (var reviewedCorrection in reviewedSectionCorrections)
                reviewedCorrection.ReviewerUserId = deletedUserId;
            context.UpdateRange(reviewedSectionCorrections);
            await context.SaveChangesAsync();

            var reportedComments = await context.GanjoorReportedComments.Where(r => r.ReportedById == userId).ToListAsync();
            foreach (var reportedComment in reportedComments)
                reportedComment.ReportedById = deletedUserId;
            context.UpdateRange(reportedComments);
            await context.SaveChangesAsync();

            var ganjoorLinks = await context.GanjoorLinks.Where(l => l.SuggestedById == userId).ToListAsync();
            foreach (var ganjoorLink in ganjoorLinks)
                ganjoorLink.SuggestedById = deletedUserId;
            context.UpdateRange(ganjoorLinks);
            await context.SaveChangesAsync();

            var reviewedGanjoorLinks = await context.GanjoorLinks.Where(l => l.ReviewerId == userId).ToListAsync();
            foreach (var reviewedGanjoorLink in reviewedGanjoorLinks)
                reviewedGanjoorLink.ReviewerId = deletedUserId;
            context.UpdateRange(reviewedGanjoorLinks);
            await context.SaveChangesAsync();

            var pinLinks = await context.PinterestLinks.Where(l => l.SuggestedById == userId).ToListAsync();
            foreach (var pinLink in pinLinks)
                pinLink.SuggestedById = deletedUserId;
            context.UpdateRange(pinLinks);
            await context.SaveChangesAsync();

            var reviewedPinLinks = await context.PinterestLinks.Where(l => l.ReviewerId == userId).ToListAsync();
            foreach (var reviewedPinLink in reviewedPinLinks)
                reviewedPinLink.ReviewerId = deletedUserId;
            context.UpdateRange(reviewedPinLinks);
            await context.SaveChangesAsync();

            var poemMusicTracks = await context.GanjoorPoemMusicTracks.Where(m => m.SuggestedById == userId).ToListAsync();
            foreach (var poemMusicTrack in poemMusicTracks)
                poemMusicTrack.SuggestedById = deletedUserId;
            context.UpdateRange(poemMusicTracks);
            await context.SaveChangesAsync();

            var snapshots = await context.GanjoorPageSnapshots.Where(s => s.MadeObsoleteByUserId == userId).ToListAsync();
            foreach (var snapshot in snapshots)
                snapshot.MadeObsoleteByUserId = deletedUserId;
            context.UpdateRange(snapshots);
            await context.SaveChangesAsync();

            var translations = await context.GanjoorPoemTranslations.Where(t => t.UserId == userId).ToListAsync();
            foreach (var translation in translations)
                translation.UserId = deletedUserId;
            context.UpdateRange(translations);
            await context.SaveChangesAsync();

            var suggestedPoetNotes = await context.GanjoorPoetSuggestedSpecLines.Where(s => s.SuggestedById == userId).ToListAsync();
            foreach (var suggestedPoetNote in suggestedPoetNotes)
                suggestedPoetNote.SuggestedById = deletedUserId;
            context.UpdateRange(suggestedPoetNotes);
            await context.SaveChangesAsync();

            var suggestedPoetPhotos = await context.GanjoorPoetSuggestedPictures.Where(s => s.SuggestedById == userId).ToListAsync();
            foreach (var suggestedPoetPhoto in suggestedPoetPhotos)
                suggestedPoetPhoto.SuggestedById = deletedUserId;
            context.UpdateRange(suggestedPoetPhotos);
            await context.SaveChangesAsync();


            var visits = await context.GanjoorUserPoemVisits.Where(v => v.UserId == userId).ToListAsync();
            context.RemoveRange(visits);
            await context.SaveChangesAsync();

            var bookmarks = await context.UserBookmarks.Where(b => b.RAppUserId == userId).ToListAsync();
            context.RemoveRange(bookmarks);
            await context.SaveChangesAsync();

            var uploadSessions = await context.UploadSessions.Where(s => s.UseId == userId).ToListAsync();
            context.RemoveRange(uploadSessions);
            await context.SaveChangesAsync();

            var recitations = await context.Recitations.Where(r => r.OwnerId == userId).ToListAsync();
            context.RemoveRange(recitations);
            await context.SaveChangesAsync();

            var ganjoorBookmarks = await context.GanjoorUserBookmarks.Where(b => b.UserId == userId).ToListAsync();
            context.RemoveRange(ganjoorBookmarks);
            await context.SaveChangesAsync();


            var reportedRecitaions = await context.RecitationErrorReports.Where(r => r.ReporterId == userId).ToListAsync();
            context.RemoveRange(reportedRecitaions);
            await context.SaveChangesAsync();

            var comments = await context.GanjoorComments.Where(c => c.UserId == userId).ToListAsync();
            foreach (var comment in comments)
            {
                //await _ganjoorService.DeleteMyComment(userId, comment.Id);/*had error in service initializtion, so done it in the dirty way*/
                await _DeleteComment(context, comment.Id);
            }

            var recitationsVotes = await context.RecitationUserUpVotes.Where(c => c.UserId == userId).ToListAsync();
            foreach (var vote in recitationsVotes)
            {
                int poemId = await context.Recitations.AsNoTracking().Where(r => r.Id == vote.RecitationId).Select(r => r.GanjoorPostId).SingleAsync();
                context.Remove(vote);
                await context.SaveChangesAsync();
                await _ReOrderPoemRecitationsAsync(context, poemId);
            }


            return await base.RemoveUserData(userId);//notifications are deleted here, some of these operations might produce new notifications
        }

        private async Task<RServiceResult<bool>> _DeleteComment(RMuseumDbContext context, int commentId)
        {
            GanjoorComment comment = await context.GanjoorComments.Where(c => c.Id == commentId).SingleOrDefaultAsync();
            if (comment == null)
            {
                return new RServiceResult<bool>(false); //not found
            }

            foreach(var reply in await _FindReplies(context, comment))
            {
                await _DeleteComment(context, reply.Id);
            }


            context.GanjoorComments.Remove(comment);
            await _context.SaveChangesAsync();

            return new RServiceResult<bool>(true);
        }

        private async Task<List<GanjoorComment>> _FindReplies(RMuseumDbContext context, GanjoorComment comment)
        {
            return await context.GanjoorComments.Where(c => c.InReplyToId == comment.Id).AsNoTracking().ToListAsync();
        }

        private async Task _ReOrderPoemRecitationsAsync(RMuseumDbContext context, int poemId, bool update = true)
        {
            var recitations =
                     await context.Recitations
                         .Where(r => r.ReviewStatus == AudioReviewStatus.Approved && r.GanjoorPostId == poemId)
                         .OrderBy(r => r.Id) //this causes the oldest recirations to become the first one
                         .ToListAsync();

            List<RecitationOrderingViewModel> scores = new List<RecitationOrderingViewModel>();

            for (var i = 0; i < recitations.Count; i++)
            {
                var recitation = recitations[i];
                RecitationOrderingViewModel score = new RecitationOrderingViewModel()
                {
                    RecitationId = recitation.Id,
                    EarlynessAdvantage = recitations.Count - 1 - i,
                    InitialScore = recitations[i].InitialScore,
                    UpVotes = await context.RecitationUserUpVotes.AsNoTracking().Where(r => r.RecitationId == recitation.Id && r.UserId != recitation.OwnerId)
                    .CountAsync(),
                    Mistakes = await context.RecitationApprovedMistakes.AsNoTracking().Where(m => m.RecitationId == recitation.Id).SumAsync(m => m.NumberOfLinesAffected)
                };


                score.TotalScores = score.EarlynessAdvantage
                    +
                    score.InitialScore
                     + score.UpVotes
                     - (5 * score.Mistakes);

                //audio order is used as a temporary variable in the following line and soon is getting replaced by computed value
                recitation.AudioOrder = score.TotalScores;

                scores.Add(score);
            }

            recitations.Sort((a, b) => b.AudioOrder.CompareTo(a.AudioOrder));
            for (var i = 0; i < recitations.Count; i++)
            {
                recitations[i].AudioOrder = i + 1;

                scores.Where(s => s.RecitationId == recitations[i].Id).Single().ComputedOrder = i + 1;

                if (update)
                {
                    context.Update(recitations[i]);
                }

            }

            if (update)
                await context.SaveChangesAsync();
        }
    }
}
