using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor.ViewModels;
using System.Net.Http;
using System.Threading.Tasks;

namespace GanjooRazor.Areas.Admin.Pages
{
    public class EarlierVersionModel : PageModel
    {
        /// <summary>
        /// last message
        /// </summary>
        public string LastMessage { get; set; }

        /// <summary>
        /// model
        /// </summary>
        [BindProperty]
        public GanjoorModifyPageViewModel EarlierVersion { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            LastMessage = "";

            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/oldversion/{Request.Query["id"]}");
                    if (!response.IsSuccessStatusCode)
                    {
                        LastMessage = await response.Content.ReadAsStringAsync();
                    }

                    response.EnsureSuccessStatusCode();

                    EarlierVersion = JsonConvert.DeserializeObject<GanjoorModifyPageViewModel>(await response.Content.ReadAsStringAsync());

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
