using System.Net.Http;
using System.Threading.Tasks;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using RSecurityBackend.Models.Auth.ViewModels;

namespace GanjooRazor.Areas.Admin.Pages
{
    public class ExamineUserModel : PageModel
    {

        public PublicRAppUser UserInfo { get; set; }

        /// <summary>
        /// Last Error
        /// </summary>
        public string LastError { get; set; }

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
                    }
                    else
                    {
                        LastError = await userInfoResponse.Content.ReadAsStringAsync();
                    }
                }
                else
                {
                    LastError = "لطفا از گنجور خارج و مجددا به آن وارد شوید.";
                }
            }
            return Page();
        }
    }
}
