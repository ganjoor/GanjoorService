using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using RMuseum.DbContext;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Auth.Db;
using RSecurityBackend.Models.Auth.ViewModels;
using RSecurityBackend.Models.Generic;
using RSecurityBackend.Services;
using RSecurityBackend.Services.Implementation;
using System;
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
        /// Sign Up Email Subject
        /// </summary>
        /// <returns>
        /// subject
        /// </returns>
        /// <param name="op"></param>
        /// <param name="secretCode"></param>
        public override string GetEmailSubject(RVerifyQueueType op, string secretCode)
        {
            string opString = op == RVerifyQueueType.SignUp ? "ثبت نام" : op == RVerifyQueueType.ForgotPassword ? "بازیابی کلمهٔ عبور" : "حذف کاربر";
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
            string opString = op == RVerifyQueueType.SignUp ? "ثبت نام" : op == RVerifyQueueType.ForgotPassword ? "بازیابی کلمهٔ عبور" : "حذف کاربر";
            string ifNot = op == RVerifyQueueType.SignUp ? "اگر در گنجور ثبت نام نکرده‌اید لطفاً این نامه را نادیده بگیرید."
                                : op == RVerifyQueueType.ForgotPassword ?
                                "اگر در گنجور فراموشی گذرواژه را نزده‌اید یا گذرواژه‌تان را به خاطر آوردید لطفاً این نامه را نادیده بگیرید."
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
               $"<title>کد {opString} شما در گنجور: {secretCode}</title>"
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
            RAppUser unmodifiedUserInfo = await _userManager.FindByIdAsync(userId.ToString());
            if (unmodifiedUserInfo == null)
            {
                return new RServiceResult<bool>(false, "کاربر مورد نظر یافت نشد");
            }

            string nickName = unmodifiedUserInfo.NickName;

            RServiceResult<bool> res = await base.ModifyUser(userId, updateUserInfo);
            if(res.Result)
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
    }
}
