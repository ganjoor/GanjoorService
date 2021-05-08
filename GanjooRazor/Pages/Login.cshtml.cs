using GanjooRazor.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using RMuseum.Models.Auth.Memory;
using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Models.Auth.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace GanjooRazor.Pages
{
    public class LoginModel : PageModel
    {

        [BindProperty]
        public LoginViewModel LoginViewModel { get; set; }

        public bool LoggedIn { get; set; }

        public string UserFriendlyName { get; set; }

        public string LastError { get; set; }

        public string RedirectUrl { get; set; }

        public void OnGet()
        {
            UserFriendlyName = Request.Cookies["Name"];
            LoggedIn = !string.IsNullOrEmpty(UserFriendlyName);
            LastError = Request.Query["error"];
            RedirectUrl = Request.Query["redirect"];
            if (string.IsNullOrEmpty(RedirectUrl))
            {
                RedirectUrl = "/";
            }

        }

        public async Task<IActionResult> OnPostAsync()
        {
            RedirectUrl = Request.Query["redirect"];
            if (string.IsNullOrEmpty(RedirectUrl))
            {
                RedirectUrl = "/";
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            LoginViewModel.ClientAppName = "GanjooRazor";
            LoginViewModel.Language = "fa-IR";

            using (HttpClient secureClient = new HttpClient())
            {
                if(string.IsNullOrEmpty(LoginViewModel.Username))
                {
                    if(await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                    {
                        var logoutUrl = $"{APIRoot.Url}/api/users/delsession?userId={Request.Cookies["UserId"]}&sessionId={Request.Cookies["SessionId"]}";
                        await secureClient.DeleteAsync(logoutUrl);
                    }
                    

                    var cookieOption = new CookieOptions()
                    {
                        Expires = DateTime.Now.AddDays(-1)
                    };
                    foreach (var cookieName in new string[] { "UserId", "SessionId", "Token", "Username", "Name", "NickName", "CanEdit" })
                    {
                        if(Request.Cookies[cookieName] != null)
                        {
                            Response.Cookies.Append(cookieName, "", cookieOption);
                        }
                    }

                    

                    return Page();
                }
                else
                {
                    var stringContent = new StringContent(JsonConvert.SerializeObject(LoginViewModel), Encoding.UTF8, "application/json");
                    var loginUrl = $"{APIRoot.Url}/api/users/login";
                    var response = await secureClient.PostAsync(loginUrl, stringContent);

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        LastError = await response.Content.ReadAsStringAsync();
                        return Page();
                    }

                    LoggedOnUserModel loggedOnUser = JsonConvert.DeserializeObject<LoggedOnUserModel>(await response.Content.ReadAsStringAsync());

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

                }

            }

            LastError = "Success!";


            return Redirect(RedirectUrl);
        }
    }
}
