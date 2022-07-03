using GanjooRazor.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using RMuseum.Models.Auth.Memory;
using RMuseum.Models.Auth.ViewModel;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Models.Auth.ViewModels;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GanjooRazor.Pages
{
    public class LoginPartialEnabledPageModel : PageModel
    {
        /// <summary>
        /// is logged on
        /// </summary>
        public bool LoggedIn { get; set; }

        [BindProperty]
        public LoginViewModel LoginViewModel { get; set; }

        /// <summary>
        /// Corresponding Ganjoor Page
        /// </summary>
        public GanjoorPageCompleteViewModel GanjoorPage { get; set; }

        /// <summary>
        /// logout
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnPostLogoutAsync()
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            if (!string.IsNullOrEmpty(Request.Cookies["SessionId"]) && !string.IsNullOrEmpty(Request.Cookies["UserId"]))
            {
                using (HttpClient secureClient = new HttpClient())
                {
                    if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                    {
                        var logoutUrl = $"{APIRoot.Url}/api/users/delsession?userId={Request.Cookies["UserId"]}&sessionId={Request.Cookies["SessionId"]}";
                        await secureClient.DeleteAsync(logoutUrl);
                    }
                }
            }


            var cookieOption = new CookieOptions()
            {
                Expires = DateTime.Now.AddDays(-1)
            };
            foreach (var cookieName in new string[] { "UserId", "SessionId", "Token", "Username", "Name", "NickName", "CanEdit", "KeepHistory" })
            {
                if (Request.Cookies[cookieName] != null)
                {
                    Response.Cookies.Append(cookieName, "", cookieOption);
                }
            }


            return Redirect(Request.Path);
        }

        /// <summary>
        /// Login
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnPostLoginAsync()
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            LoginViewModel.ClientAppName = "GanjooRazor";
            LoginViewModel.Language = "fa-IR";

            var stringContent = new StringContent(JsonConvert.SerializeObject(LoginViewModel), Encoding.UTF8, "application/json");
            var loginUrl = $"{APIRoot.Url}/api/users/login";
            var response = await _httpClient.PostAsync(loginUrl, stringContent);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                return Redirect($"/login?redirect={Request.Path}&error={JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync())}");
            }

            LoggedOnUserModelEx loggedOnUser = JsonConvert.DeserializeObject<LoggedOnUserModelEx>(await response.Content.ReadAsStringAsync());

            var cookieOption = new CookieOptions()
            {
                Expires = DateTime.Now.AddDays(365),
            };

            Response.Cookies.Append("UserId", loggedOnUser.User.Id.ToString(), cookieOption);
            Response.Cookies.Append("SessionId", loggedOnUser.SessionId.ToString(), cookieOption);
            Response.Cookies.Append("Token", loggedOnUser.Token, cookieOption);
            Response.Cookies.Append("Username", loggedOnUser.User.Username, cookieOption);
            Response.Cookies.Append("Name", $"{loggedOnUser.User.FirstName} {loggedOnUser.User.SureName}", cookieOption);
            Response.Cookies.Append("NickName", $"{loggedOnUser.User.NickName}", cookieOption);
            Response.Cookies.Append("KeepHistory", $"{loggedOnUser.KeepHistory}", cookieOption);

            bool canEditContent = false;
            var ganjoorEntity = loggedOnUser.SecurableItem.Where(s => s.ShortName == RMuseumSecurableItem.GanjoorEntityShortName).SingleOrDefault();
            if (ganjoorEntity != null)
            {
                var op = ganjoorEntity.Operations.Where(o => o.ShortName == SecurableItem.ModifyOperationShortName).SingleOrDefault();
                if (op != null)
                {
                    canEditContent = op.Status;
                }
            }

            Response.Cookies.Append("CanEdit", canEditContent.ToString(), cookieOption);


            return Redirect(Request.Path);
        }

        /// <summary>
        /// HttpClient instance
        /// </summary>
        protected readonly HttpClient _httpClient;

        public LoginPartialEnabledPageModel(
            HttpClient httpClient
            )
        {
            _httpClient = httpClient;
        }
    }
}
