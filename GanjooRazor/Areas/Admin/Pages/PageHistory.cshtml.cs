using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor.ViewModels;
using System.Net.Http;
using System.Threading.Tasks;

namespace GanjooRazor.Areas.Admin.Pages
{
    public class PageHistoryModel : PageModel
    {
        /// <summary>
        /// last message
        /// </summary>
        public string LastMessage { get; set; }

        public GanjoorPageSnapshotSummaryViewModel[] OlderVersions { get; set; }
        public async Task<IActionResult> OnGetAsync()
        {
            LastMessage = "";

            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/page/oldversions/{Request.Query["id"]}");
                    if (!response.IsSuccessStatusCode)
                    {
                        LastMessage = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                        return Page();
                    }

                    OlderVersions = JsonConvert.DeserializeObject<GanjoorPageSnapshotSummaryViewModel[]>(await response.Content.ReadAsStringAsync());

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
