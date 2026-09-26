using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GanjooRazor.Pages;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using RSecurityBackend.Models.Auth.ViewModels;

namespace GanjooRazor.Areas.User.Pages
{
    /// <summary>
    /// Lets a logged-on user see every session (device/browser) currently able to use their
    /// account, and revoke any one of them - the self-service alternative to a hard absolute
    /// session ceiling: instead of forcing everyone to relogin periodically, the user can look at
    /// this list and kill a session they don't recognize (a lost/stolen device, a shared computer
    /// they forgot to log out of, ...) whenever they suspect something.
    /// </summary>
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class MySessionsModel : GanjoorPageModelBase
    {
        /// <summary>
        /// Last Error
        /// </summary>
        public string LastError { get; set; }

        /// <summary>
        /// the user's own sessions, most recently active first
        /// </summary>
        public List<PublicRUserSession> Sessions { get; set; }

        /// <summary>
        /// the SessionId cookie of the browser rendering this page, so the view can mark it as
        /// "this device" and the delete handler can special-case removing it.
        /// </summary>
        public Guid CurrentSessionId { get; set; }

        public MySessionsModel(HttpClient httpClient) : base(httpClient)
        {
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(Request.Cookies["Token"]))
                return Redirect("/");

            Guid.TryParse(Request.Cookies["SessionId"], out Guid currentSessionId);
            CurrentSessionId = currentSessionId;

            LastError = "";
            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.GetAsync($"{APIRoot.Url}/api/users/sessions?userId={Request.Cookies["UserId"]}");
                    if (!response.IsSuccessStatusCode)
                    {
                        LastError = await ReadErrorMessageAsync(response);
                        return Page();
                    }

                    Sessions = JArray.Parse(await response.Content.ReadAsStringAsync())
                        .ToObject<List<PublicRUserSession>>()
                        .OrderByDescending(s => s.LastRenewal)
                        .ToList();
                }
                else
                {
                    LastError = NotLoggedInMessage;
                }
            return Page();
        }

        /// <summary>
        /// Deletes one of the user's own sessions (the API only allows deleting your own session
        /// here without the extra user:delothersession permission - see AppUserControllerBase.Logout).
        /// If the session being deleted is the one this very request is authenticated with, this
        /// browser's auth cookies are cleared too (mirroring LoginPartialEnabledPageModel's
        /// OnPostLogoutAsync), so it doesn't keep showing a "logged in" page for a session that no
        /// longer exists server-side; the caller (see the view) then redirects home instead of just
        /// removing the row from the list.
        /// </summary>
        public async Task<IActionResult> OnDeleteSessionAsync(Guid id)
        {
            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.DeleteAsync($"{APIRoot.Url}/api/users/delsession?userId={Request.Cookies["UserId"]}&sessionId={id}");
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return new BadRequestObjectResult(await ReadErrorMessageAsync(response));
                    }
                }
                else
                {
                    return new BadRequestObjectResult(NotLoggedInMessage);
                }
            }

            bool loggedOutSelf = Request.Cookies["SessionId"] == id.ToString();
            if (loggedOutSelf)
            {
                var cookieOption = new CookieOptions()
                {
                    Expires = DateTime.Now.AddDays(-1),
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                };
                foreach (var cookieName in new string[] { "UserId", "SessionId", "Token", "Username", "Name", "NickName", "CanEdit", "KeepHistory", "CanTranslate" })
                {
                    if (Request.Cookies[cookieName] != null)
                    {
                        Response.Cookies.Append(cookieName, "", cookieOption);
                    }
                }
            }

            return new JsonResult(new { loggedOutSelf });
        }
    }
}
