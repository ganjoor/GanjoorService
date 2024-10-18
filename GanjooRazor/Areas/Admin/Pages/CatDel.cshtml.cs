using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor.ViewModels;
using System.Net.Http;
using System.Threading.Tasks;

namespace GanjooRazor.Areas.Admin.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class CatDelModel : PageModel
    {
        /// <summary>
        /// HttpClient instance
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="httpClient"></param>
        public CatDelModel(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// category
        /// </summary>
        public GanjoorPoetCompleteViewModel Cat { get; set; }


        /// <summary>
        /// last result
        /// </summary>
        public string LastResult { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(Request.Cookies["Token"]))
                return Redirect("/");

            LastResult = "";
            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/cat/{Request.Query["id"]}");
            if (!response.IsSuccessStatusCode)
            {
                LastResult = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                return Page();
            }
            Cat = JObject.Parse(await response.Content.ReadAsStringAsync()).ToObject<GanjoorPoetCompleteViewModel>();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            LastResult = "";
            using (HttpClient secureClient = new HttpClient())
            {
                await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response);

  

                var response = await secureClient.DeleteAsync($"{APIRoot.Url}/api/ganjoor/cat/{Request.Query["id"]}");

                if (!response.IsSuccessStatusCode)
                {
                    LastResult = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                }
                else
                {
                    LastResult = "عملیات حذف بخش انجام شد.";
                }


                return Page();

            }
        }

    }
}
