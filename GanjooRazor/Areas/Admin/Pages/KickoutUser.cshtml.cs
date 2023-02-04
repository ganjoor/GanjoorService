using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using RSecurityBackend.Models.Auth.ViewModels;

namespace GanjooRazor.Areas.Admin.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class KickoutUserModel : PageModel
    {
        public PublicRAppUser UserInfo { get; set; }

        /// <summary>
        /// Last Error
        /// </summary>
        public string LastResult { get; set; }

        [BindProperty]
        public UserCauseViewModel UserCauseViewModel { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(Request.Cookies["Token"]))
                return Redirect("/");

            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var userInfoResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/users/{Request.Query["id"]}");
                    if (userInfoResponse.IsSuccessStatusCode)
                    {
                        UserInfo = JsonConvert.DeserializeObject<PublicRAppUser>(await userInfoResponse.Content.ReadAsStringAsync());

                        UserCauseViewModel = new UserCauseViewModel()
                        {
                            UserId = (Guid)UserInfo.Id,
                            Cause = "نقض قوانین حاشیه‌گذاری"
                        };
                    }
                    else
                    {
                        LastResult = JsonConvert.DeserializeObject<string>(await userInfoResponse.Content.ReadAsStringAsync());
                    }
                }
                else
                {
                    LastResult = "لطفاً از گنجور خارج و مجددا به آن وارد شوید.";
                }
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            LastResult = "";
            using (HttpClient secureClient = new HttpClient())
            {
                await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response);


                HttpResponseMessage response = await secureClient.PostAsync($"{APIRoot.Url}/api/users/kickout", new StringContent(JsonConvert.SerializeObject(UserCauseViewModel), Encoding.UTF8, "application/json"));
                if(response.IsSuccessStatusCode)
                {
                    LastResult = "کاربر حذف شد.";
                }
                else
                {
                    LastResult = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                }

                return Page();

            }
        }
    }
}
