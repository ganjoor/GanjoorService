using Microsoft.AspNetCore.Mvc;
using System.Net.Http;

namespace GanjooRazor.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class LoginModel : LoginPartialEnabledPageModel
    {

        public LoginModel(HttpClient httpClient) : base(httpClient) { }
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
    }
}
