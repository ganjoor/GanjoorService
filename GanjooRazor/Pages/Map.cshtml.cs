using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor.ViewModels;

namespace GanjooRazor.Pages
{
    public class MapModel : PageModel
    {

        public string LastError { get; set; }

        private async Task<List<GanjoorPoetViewModel>> _PreparePoets()
        {
            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poets");
            if (!response.IsSuccessStatusCode)
            {
                LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                return new List<GanjoorPoetViewModel>();
            }
            var poets = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoetViewModel>>();

            var poetsWithBirthPlaces = poets.Where(p => !string.IsNullOrEmpty(p.BirthPlace)).ToList();

            foreach (var poet in poetsWithBirthPlaces)
            {
                poet.ImageUrl = $"{APIRoot.InternetUrl}{poet.ImageUrl}";
            }

            return poetsWithBirthPlaces;
        }


        public List<GanjoorCenturyViewModel> PoetGroupsWithBirthPlaces { get; set; }

        private async Task _PreparePoetGroups()
        {
            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/centuries");
            if (!response.IsSuccessStatusCode)
            {
                LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                return;
            }
            var poetGroups = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorCenturyViewModel>>();
            PoetGroupsWithBirthPlaces = new List<GanjoorCenturyViewModel>();
            PoetGroupsWithBirthPlaces.Add
                (
                new GanjoorCenturyViewModel()
                {
                    Id = 0,
                    Name = "همهٔ اعصار",
                    ShowInTimeLine = true,
                    Poets = await _PreparePoets()
                }
                );
            GanjoorCenturyViewModel prePoetGroup = null;
            foreach (var poetGroup in poetGroups)
            {
                if (poetGroup.Id == 0)
                    continue;
                poetGroup.Poets = poetGroup.Poets.Where(p => !string.IsNullOrEmpty(p.BirthPlace)).ToList();
                if (poetGroup.Poets.Count > 0)
                {
                    foreach (var poet in poetGroup.Poets)
                    {
                        poet.ImageUrl = $"{APIRoot.InternetUrl}{poet.ImageUrl}";
                    }
                    if (!poetGroup.ShowInTimeLine && prePoetGroup != null)
                    {
                        prePoetGroup.Poets.AddRange(poetGroup.Poets);
                    }
                    else
                    {
                        PoetGroupsWithBirthPlaces.Add(poetGroup);
                        prePoetGroup = poetGroup;
                    }
                }
            }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["GoogleAnalyticsCode"] = _configuration["GoogleAnalyticsCode"];
            await _PreparePoetGroups();
            return Page();
        }

        /// <summary>
        /// HttpClient instance
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// configration file reader (appsettings.json)
        /// </summary>
        private readonly IConfiguration _configuration;

        public MapModel(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }
    }
}
