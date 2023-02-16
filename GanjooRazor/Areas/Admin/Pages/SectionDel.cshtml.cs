using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace GanjooRazor.Areas.Admin.Pages
{

    [IgnoreAntiforgeryToken(Order = 1001)]
    public class SectionDelModel : PageModel
    {
        /// <summary>
        /// HttpClient instance
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="httpClient"></param>
        public SectionDelModel(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// last result
        /// </summary>
        public string LastResult { get; set; }

        
        public IActionResult OnGet()
        {
            if (string.IsNullOrEmpty(Request.Cookies["Token"]))
                return Redirect("/");

            LastResult = "";

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            LastResult = "";
            using (HttpClient secureClient = new HttpClient())
            {
                await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response);

                HttpResponseMessage response = await secureClient.DeleteAsync($"{APIRoot.Url}/api/ganjoor/section/{Request.Query["poemId"]}/{Request.Query["sectionIndex"]}");
                if (!response.IsSuccessStatusCode)
                {
                    LastResult = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                    return Page();
                }

                LastResult = "ﬁÿ⁄Â Õ–› ‘œ.";

                return Page();

            }
        }
    }
}
