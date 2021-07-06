using System.Net.Http;
using System.Threading.Tasks;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
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
        /// memory cache
        /// </summary>
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="memoryCache"></param>
        public PageDelModel(HttpClient httpClient, IMemoryCache memoryCache)
        {
            _httpClient = httpClient;
            _memoryCache = memoryCache;
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
            var pageQuery = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/page?url={Request.Query["url"]}");
            pageQuery.EnsureSuccessStatusCode();

            PageInformation = JObject.Parse(await pageQuery.Content.ReadAsStringAsync()).ToObject<GanjoorPageCompleteViewModel>();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            LastResult = "";
            using (HttpClient secureClient = new HttpClient())
            {
                await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response);

                HttpResponseMessage response = await secureClient.DeleteAsync($"{APIRoot.Url}/api/ganjoor/page/cache/{Request.Query["id"]}");
                response.EnsureSuccessStatusCode();


                response = await secureClient.DeleteAsync($"{APIRoot.Url}/api/ganjoor/page/{Request.Query["id"]}");

                if(!response.IsSuccessStatusCode)
                {
                    LastResult = await response.Content.ReadAsStringAsync();
                }
                else
                {
                    response.EnsureSuccessStatusCode();
                    LastResult = "عملیات حذف صفحه انجام شد.";
                }
               

                return Page();

            }
        }
    }
}
