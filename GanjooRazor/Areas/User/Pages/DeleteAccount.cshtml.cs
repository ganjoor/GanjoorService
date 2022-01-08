using GanjooRazor.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RSecurityBackend.Models.Auth.ViewModels;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GanjooRazor.Areas.User.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class DeleteAccountModel : PageModel
    {
        /// <summary>
        /// fatal error
        /// </summary>
        public string FatalError { get; set; }

        public bool Step1 { get; set; }

        [BindProperty]
        public SelfDeleteViewModel DeleteViewModel { get; set; }

        [BindProperty]
        public string Secret { get; set; }

        /// <summary>
        /// configuration
        /// </summary>
        private readonly IConfiguration _configuration;

        public DeleteAccountModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task<IActionResult> OnGetAsync()
        {
            FatalError = "";
            if (!string.IsNullOrEmpty(Request.Query["secret"]))
                return await OnPostFinalizeDeleteAsync(Request.Query["secret"]);

            Step1 = true;

            return Page();
        }

        public async Task<IActionResult> OnPostSendEmailAsync(SelfDeleteViewModel deleteViewModel)
        {
            Step1 = true;
            FatalError = "";
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    deleteViewModel.CallbackUrl = $"{_configuration["SiteUrl"]}/User/DeleteAccount";
                    HttpResponseMessage response = await secureClient.PostAsync(
                        $"{APIRoot.Url}/api/users/selfdelete/start",
                        new StringContent(JsonConvert.SerializeObject(deleteViewModel),
                        Encoding.UTF8,
                        "application/json"
                        ));
                    if(!response.IsSuccessStatusCode)
                    {
                        FatalError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                    }
                    else
                    {
                        Step1 = false;
                    }
                }
                else
                {
                    FatalError = "لطفا از گنجور خارج و مجددا به آن وارد شوید.";
                }
            }

            return Page();
        }



        public async Task<IActionResult> OnPostFinalizeDeleteAsync(string secret)
        {
            FatalError = "";
            Step1 = false;
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.DeleteAsync(
                        $"{APIRoot.Url}/api/users/selfdelete/finalize/{secret}"
                        );
                    if (!response.IsSuccessStatusCode)
                    {
                        FatalError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                    }
                    else
                    {
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


                        return Redirect("/");
                    }

                }
                else
                {
                    FatalError = "لطفا از گنجور خارج و مجددا به آن وارد شوید.";
                }
            }

            return Page();
        }
    }
}
