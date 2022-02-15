using System.Net.Http;
using System.Threading.Tasks;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor.ViewModels;

namespace GanjooRazor.Areas.Admin.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class PageDelModel : PageModel
    {
        /// <summary>
        /// HttpClient instance
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="httpClient"></param>
        public PageDelModel(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// page
        /// </summary>
        public GanjoorPageCompleteViewModel PageInformation { get; set; }


        /// <summary>
        /// last result
        /// </summary>
        public string LastResult { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(Request.Cookies["Token"]))
                return Redirect("/");

            LastResult = "";
            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/page?url={Request.Query["url"]}");
            if (!response.IsSuccessStatusCode)
            {
                LastResult = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                return Page();
            }
            PageInformation = JObject.Parse(await response.Content.ReadAsStringAsync()).ToObject<GanjoorPageCompleteViewModel>();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            LastResult = "";
            using (HttpClient secureClient = new HttpClient())
            {
                await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response);

                HttpResponseMessage response = await secureClient.DeleteAsync($"{APIRoot.Url}/api/ganjoor/page/cache/{Request.Query["id"]}");
                if (!response.IsSuccessStatusCode)
                {
                    LastResult = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                    return Page();
                }


                response = await secureClient.DeleteAsync($"{APIRoot.Url}/api/ganjoor/page/{Request.Query["id"]}");

                if(!response.IsSuccessStatusCode)
                {
                    LastResult = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                }
                else
                {
                    LastResult = "عملیات حذف صفحه انجام شد.";
                }
               

                return Page();

            }
        }
    }
}
