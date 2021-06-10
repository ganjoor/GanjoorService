using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor.ViewModels;

namespace GanjooRazor.Areas.Admin.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class PoetModel : PageModel
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
        public PoetModel(HttpClient httpClient, IMemoryCache memoryCache)
        {
            _httpClient = httpClient;
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// last result
        /// </summary>
        public string LastResult { get; set; }


        [BindProperty]
        public GanjoorPoetViewModel Poet { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            LastResult = "";
            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poet/{Request.Query["id"]}");

            response.EnsureSuccessStatusCode();

            var poet = JsonConvert.DeserializeObject<GanjoorPoetCompleteViewModel>(await response.Content.ReadAsStringAsync());


            Poet = poet.Poet;


            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            LastResult = "";
            using (HttpClient secureClient = new HttpClient())
            {
                await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response);
                HttpResponseMessage response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/poet/{Request.Query["id"]}", new StringContent(JsonConvert.SerializeObject(Poet), Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();

                var cacheKey1 = $"/api/ganjoor/poets";
                if (_memoryCache.TryGetValue(cacheKey1, out List<GanjoorPoetViewModel> poets))
                {
                    _memoryCache.Remove(cacheKey1);
                }

                var cacheKey2 = $"/api/ganjoor/poet/{Request.Query["id"]}";
                if (_memoryCache.TryGetValue(cacheKey2, out GanjoorPoetCompleteViewModel poet))
                {
                    _memoryCache.Remove(cacheKey2);
                }

                LastResult = "ویرایش انجام شد.";

                return Page();
            }
        }
    }
}
