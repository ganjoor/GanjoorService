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

        private async Task _PreparePoets()
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

        
        public List<GanjoorCenturyViewModel> PoetGroupsWithBirthPlaces { get; set; }

        private async Task _PreparePoetGroups()
        {
            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/centuries");
            response.EnsureSuccessStatusCode();
            var poetGroups = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorCenturyViewModel>>();
            PoetGroupsWithBirthPlaces = new List<GanjoorCenturyViewModel>();
            foreach (var poetGroup in poetGroups)
            {
                poetGroup.Poets = poetGroup.Poets.Where(p => !string.IsNullOrEmpty(p.BirthPlace)).ToList();
                if(poetGroup.Poets.Count > 0)
                {
                    PoetGroupsWithBirthPlaces.Add(poetGroup);
                }
            }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await _PreparePoets();
            await _PreparePoetGroups();
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

       
        public MapModel(IConfiguration configuration,
            HttpClient httpClient,
            IMemoryCache memoryCache
            )
        {
            _httpClient = httpClient;
            _memoryCache = memoryCache;
        }
    }
}
