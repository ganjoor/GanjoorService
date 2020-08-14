using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RSecurityBackend.Services;
using RSecurityBackend.Controllers;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Audit.WebApi;

namespace RMuseum.Controllers
{
    /// <summary>
    /// User login/logout/register/...
    /// </summary>
    [Produces("application/json")]
    [Route("api/users")]
    public class AppUserController : AppUserControllerBase
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="appUserService"></param>
        /// <param name="httpContextAccessor"></param>
        /// <param name="userPermissionChecker"></param>
        /// <param name="emailSender"></param>
        /// <param name="imageFileService"></param>
        /// <param name="captchaService"></param>
        public AppUserController(IConfiguration configuration, IAppUserService appUserService, IHttpContextAccessor httpContextAccessor, IUserPermissionChecker userPermissionChecker, IEmailSender emailSender, IImageFileService imageFileService, ICaptchaService captchaService)
            : base(configuration, appUserService, httpContextAccessor, userPermissionChecker, emailSender, imageFileService, captchaService)
        {
            
        }      



        

        /// <summary>
        /// Sign Up Email Subject
        /// </summary>
        /// <returns>
        /// subject
        /// </returns>
        /// <param name="secretCode"></param>
        protected override string GetSignUpEmailSubject(string secretCode)
        {
            return $"{secretCode} کد ثبت نام شما در گنجینهٔ گنجور";
        }

        /// <summary>
        /// Sign Up Email Html Content
        /// </summary>
        /// <param name="secretCode"></param>
        /// <returns>html content</returns>
        protected override string GetSignUpEmailHtmlContent(string secretCode)
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
               $"<title>کد ثبت نام شما در گنجینهٔ گنجور: {secretCode}</title>"
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
                "<img src=\"https://i.ganjoor.net/gm.gif\" alt=\"گنجینهٔ گنجور\" width=\"150\" height=\"150\" style=\"display: block;\" />"
                +
                "</td>"
                +
                "</tr>"
                +
                "<tr><td>"
                +
                $"<p style=\"font:normal 12px tahoma;direction:rtl\">لطفاً جهت تکمیل ثبت نام در گنجینهٔ گنجور <a href=\"{SignupCallbackUrl}?secret={secretCode}\">اینجا</a> کلیک کنید یا اگر صفحهٔ ثبت نام هنوز باز است کد <strong>{secretCode}</strong> را در آن وارد کنید.</p>"
                +
                "</td></tr>"
                +
                "<tr><td>"
                +
                "<p style=\"font:normal 12px tahoma;direction:rtl\">اگر در گنجینهٔ گنجور ثبت نام نکرده‌اید لطفاً این نامه را نادیده بگیرید. به این نامه پاسخ ندهید، کسی پاسخگوی شما نخواهد بود.</p>"
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
        protected override string GetForgotPasswordEmailSubject(string secretCode)
        {
            return $"{secretCode} کد بازیابی کلمهٔ عبور شما در گنجینهٔ گنجور";
        }

        /// <summary>
        /// Forgot Password Email Html Content
        /// </summary>
        /// <param name="secretCode"></param>
        /// <returns>html content</returns>
        protected override string GetForgotPasswordEmailHtmlContent(string secretCode)
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
              $"<title>کد تغییر گذرواژهٔ شما در گنجینهٔ گنجور: {secretCode}</title>"
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
               "<img src=\"https://i.ganjoor.net/gm.gif\" alt=\"گنجینهٔ گنجور\" width=\"150\" height=\"150\" style=\"display: block;\" />"
               +
               "</td>"
               +
               "</tr>"
               +
               "<tr><td>"
               +
               $"<p style=\"font:normal 12px tahoma;direction:rtl\">لطفاً جهت تکمیل فرایند فراموشی گذرواژه در گنجینهٔ گنجور <a href=\"{ForgotPasswordCallbackUrl}?secret={secretCode}\">اینجا</a> کلیک کنید یا اگر صفحهٔ فراموشی گذرواژه هنوز باز است کد <strong>{secretCode}</strong> را در آن وارد کنید.</p>"
               +
               "</td></tr>"
               +
               "<tr><td>"
               +
               "<p style=\"font:normal 12px tahoma;direction:rtl\">اگر در گنجینهٔ گنجور فراموشی گذرواژه را نزده‌اید یا گذرواژه‌تان را به خاطر آوردید لطفاً این نامه را نادیده بگیرید. به این نامه پاسخ ندهید، کسی پاسخگوی شما نخواهد بود.</p>"
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
        /// Is Sign-up enabled?
        /// </summary>
        /// <returns></returns>
        protected override bool IsSignupEnabled()
        {
            return true;
        }

    }
}
