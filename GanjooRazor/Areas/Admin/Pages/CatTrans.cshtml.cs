using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;

namespace GanjooRazor.Areas.Admin.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class CatTransModel : PageModel
    {
        /// <summary>
        /// last message
        /// </summary>
        public string LastMessage { get; set; }

        /// <summary>
        /// poems
        /// </summary>
        public GanjoorDuplicateViewModel[] Poems { get; set; }

        private async Task<bool> _GetCatDuplicatesAsync()
        {
            if (string.IsNullOrEmpty(Request.Query["id"]))
            {
                LastMessage = "شناسهٔ بخش مشخص نیست.";
                return false;
            }
            var id = Request.Query["id"];
            using (HttpClient secureClient = new HttpClient())
            {
                if(await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/duplicates/{id}");
                    if (!response.IsSuccessStatusCode)
                    {
                        LastMessage = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                        return false;
                    }
                    else
                    {
                        Poems = JsonConvert.DeserializeObject<GanjoorDuplicateViewModel[]>(await response.Content.ReadAsStringAsync());
                    }
                }
                else
                {
                    LastMessage = "لطفا از گنجور خارج و مجددا به آن وارد شوید.";
                    return false;
                }
            }
            return true;
        }
        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(Request.Cookies["Token"]))
                return Redirect("/");

            await _GetCatDuplicatesAsync();

            return Page();
        }
    }
}
