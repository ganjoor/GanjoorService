using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor.ViewModels;

namespace GanjooRazor.Pages
{
    public class MapModel : PageModel
    {
        /// <summary>
        /// Poets
        /// </summary>
        public List<GanjoorPoetViewModel> PoetsWithBirthPlaces { get; set; }

        private async Task preparePoets()
        {
            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poets");
            response.EnsureSuccessStatusCode();
            var poets = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoetViewModel>>();

            PoetsWithBirthPlaces = poets.Where(p => !string.IsNullOrEmpty(p.BirthPlace)).ToList();

            foreach (var poet in PoetsWithBirthPlaces)
            {
                poet.ImageUrl = $"{APIRoot.InternetUrl}{poet.ImageUrl}";
            }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await preparePoets();
            return Page();
        }

        /// <summary>
        /// HttpClient instance
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// memory cache
        /// </summary>
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// configration file reader (appsettings.json)
        /// </summary>
        private readonly IConfiguration _configuration;

        public MapModel(IConfiguration configuration,
            HttpClient httpClient,
            IMemoryCache memoryCache
            )
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _memoryCache = memoryCache;
        }
    }
}
