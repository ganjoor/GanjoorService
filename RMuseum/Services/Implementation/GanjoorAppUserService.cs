using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RMuseum.DbContext;
using RSecurityBackend.Models.Auth.Db;
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
            IConfiguration configuration)
            : base(context, userManager, signInManager, roleManager, secretGenerator, imageFileService, userRoleService, configuration)
        {
           
        }

        /// <summary>
        /// Sign Up Email Subject
        /// </summary>
        /// <returns>
        /// subject
        /// </returns>
        /// <param name="secretCode"></param>
        public override string GetSignUpEmailSubject(string secretCode)
        {
            return $"{secretCode} کد ثبت نام شما در گنجور";
        }

        /// <summary>
        /// Sign Up Email Html Content
        /// </summary>
        /// <param name="secretCode"></param>
        /// <param name="signupCallbackUrl"></param>
        /// <returns>html content</returns>
        public override string GetSignUpEmailHtmlContent(string secretCode, string signupCallbackUrl)
        {
            string content =
               "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">"
               +
               "<html xmlns=\"http://www.w3.org/1999/xhtml\" xml:lang=\"en\" lang=\"fa\">"
               +
               "<head>"
               +
               "<meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\" />"
               +
               $"<title>کد ثبت نام شما در گنجور: {secretCode}</title>"
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
                $"<p style=\"font:normal 12px tahoma;direction:rtl\">لطفاً جهت تکمیل ثبت نام در گنجور کد <strong>{secretCode}</strong> را به عنوان رمز دریافتی در صفحهٔ ثبت نام وارد کنید.</p>"
                :
                $"<p style=\"font:normal 12px tahoma;direction:rtl\">لطفاً جهت تکمیل ثبت نام در گنجور <a href=\"{signupCallbackUrl}?secret={secretCode}\">اینجا</a> کلیک کنید یا اگر صفحهٔ ثبت نام هنوز باز است کد <strong>{secretCode}</strong> را در آن وارد کنید.</p>"
                )
                +
                "</td></tr>"
                +
                "<tr><td>"
                +
                "<p style=\"font:normal 12px tahoma;direction:rtl\">اگر در گنجور ثبت نام نکرده‌اید لطفاً این نامه را نادیده بگیرید. به این نامه پاسخ ندهید، کسی پاسخگوی شما نخواهد بود.</p>"
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
        /// Forgot Password Email Subject
        /// </summary>
        /// <returns>
        /// subject
        /// </returns>
        /// <param name="secretCode"></param>
        public override string GetForgotPasswordEmailSubject(string secretCode)
        {
            return $"{secretCode} کد بازیابی کلمهٔ عبور شما در گنجور";
        }

        /// <summary>
        /// Forgot Password Email Html Content
        /// </summary>
        /// <param name="secretCode"></param>
        /// <param name="forgotPasswordCallbackUrl"></param>
        /// <returns>html content</returns>
        public override string GetForgotPasswordEmailHtmlContent(string secretCode, string forgotPasswordCallbackUrl)
        {
            string content =
              "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">"
              +
              "<html xmlns=\"http://www.w3.org/1999/xhtml\" xml:lang=\"en\" lang=\"fa\">"
              +
              "<head>"
              +
              "<meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\" />"
              +
              $"<title>کد تغییر گذرواژهٔ شما در گنجور: {secretCode}</title>"
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
               string.IsNullOrEmpty(forgotPasswordCallbackUrl) ?
               $"<p style=\"font:normal 12px tahoma;direction:rtl\">لطفاً جهت تکمیل فرایند فراموشی گذرواژه در گنجور کد <strong>{secretCode}</strong> را به عنوان رمز دریافتی در صفحهٔ بازیابی گذرواژه وارد کنید.</p>"
               :
               $"<p style=\"font:normal 12px tahoma;direction:rtl\">لطفاً جهت تکمیل فرایند فراموشی گذرواژه در گنجور <a href=\"{forgotPasswordCallbackUrl}?secret={secretCode}\">اینجا</a> کلیک کنید یا اگر صفحهٔ فراموشی گذرواژه هنوز باز است کد <strong>{secretCode}</strong> را در آن وارد کنید.</p>"
               )
               +
               "</td></tr>"
               +
               "<tr><td>"
               +
               "<p style=\"font:normal 12px tahoma;direction:rtl\">اگر در گنجور فراموشی گذرواژه را نزده‌اید یا گذرواژه‌تان را به خاطر آوردید لطفاً این نامه را نادیده بگیرید. به این نامه پاسخ ندهید، کسی پاسخگوی شما نخواهد بود.</p>"
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
                    return new RServiceResult<bool>(true); //ignore this error! because signup was succesful
                }
            }
            return res;
        }
    }
}
