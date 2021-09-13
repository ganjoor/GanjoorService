using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using RMuseum.Models.Auth.Memory;
using RMuseum.Models.Ganjoor;

namespace GanjooRazor.Areas.User.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class LanguagesModel : PageModel
    {
        public string LastMessage { get; set; }
        
        [BindProperty]
        public GanjoorLanguage Language { get; set; }

        public GanjoorLanguage[] Languages { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            LastMessage = "";
            await GanjoorSessionChecker.ApplyPermissionsToViewData(Request, Response, ViewData);
            if (!ViewData.ContainsKey($"{RMuseumSecurableItem.GanjoorEntityShortName}-{RMuseumSecurableItem.Translations}"))
            {
                LastMessage = "شما به این بخش دسترسی ندارید.";
                return Page();
            }

            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.GetAsync($"{APIRoot.Url}/api/translations/langugages");
                    if (!response.IsSuccessStatusCode)
                    {
                        LastMessage = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                        return Page();
                    }
                    response.EnsureSuccessStatusCode();

                    Languages = JsonConvert.DeserializeObject<GanjoorLanguage[]>(await response.Content.ReadAsStringAsync());

                }
                else
                {
                    LastMessage = "لطفا از گنجور خارج و مجددا به آن وارد شوید.";
                }
            }


            return Page();
        }
    }
}
