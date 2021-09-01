using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using RSecurityBackend.Models.Auth.ViewModels;

namespace GanjooRazor.Areas.Admin.Pages
{
    public class KickoutUserModel : PageModel
    {
        public PublicRAppUser UserInfo { get; set; }

        /// <summary>
        /// Last Error
        /// </summary>
        public string LastResult { get; set; }

        public async Task OnGetAsync()
        {
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
                        LastResult = await userInfoResponse.Content.ReadAsStringAsync();
                    }
                }
                else
                {
                    LastResult = "لطفا از گنجور خارج و مجددا به آن وارد شوید.";
                }
            }
        }
    }
}
