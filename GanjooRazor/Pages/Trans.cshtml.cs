using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;

namespace GanjooRazor.Pages
{
    public class TransModel : PageModel
    {
        /// <summary>
        /// HttpClient instance
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// configration file reader (appsettings.json)
        /// </summary>
        private readonly IConfiguration _configuration;


        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="configuration"></param>
        public TransModel(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }
        /// <summary>
        /// poem
        /// </summary>
        public GanjoorPoemCompleteViewModel Poem { get; set; }

        public GanjoorLanguage[] Languages { get; set; }

        public GanjoorPoemTranslationViewModel[] Translations { get; set; }

        public string ErrorMessage { get; set; }

        public int PoemId { get; set; }

        /// <summary>
        /// is logged on
        /// </summary>
        public bool LoggedIn { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            ErrorMessage = "";

            LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Token"]);

            PoemId = int.Parse(Request.Query["p"]);

            ViewData["GoogleAnalyticsCode"] = _configuration["GoogleAnalyticsCode"];

            HttpResponseMessage responseLanguages = await _httpClient.GetAsync($"{APIRoot.Url}/api/translations/languages");
            if (!responseLanguages.IsSuccessStatusCode)
            {
                ErrorMessage = JsonConvert.DeserializeObject<string>(await responseLanguages.Content.ReadAsStringAsync());
                return Page();
            }
            responseLanguages.EnsureSuccessStatusCode();

            var allLanguages = JsonConvert.DeserializeObject<GanjoorLanguage[]>(await responseLanguages.Content.ReadAsStringAsync());

            HttpResponseMessage response = await _httpClient.GetAsync($"{APIRoot.Url}/api/translations/poem/{PoemId}");
            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                return Page();
            }
            response.EnsureSuccessStatusCode();

            Translations = JsonConvert.DeserializeObject<GanjoorPoemTranslationViewModel[]>(await response.Content.ReadAsStringAsync());

            List<GanjoorLanguage> poemLanguages = new List<GanjoorLanguage>();
            foreach (var lang in allLanguages)
            {
                if(Translations.Where(t => t.LanguageId == lang.Id).FirstOrDefault() != null)
                {
                    poemLanguages.Add(lang);
                }
            }

            Languages = poemLanguages.ToArray();


            var responsePoem = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poem/{PoemId}?verseDetails=true&catInfo=true&rhymes=false&recitations=false&images=false&songs=false&comments=false&navigation=false");
            if(!responsePoem.IsSuccessStatusCode)
            {
                ErrorMessage = JsonConvert.DeserializeObject<string>(await responsePoem.Content.ReadAsStringAsync());
                return Page();
            }
            
            responsePoem.EnsureSuccessStatusCode();

            Poem = JsonConvert.DeserializeObject<GanjoorPoemCompleteViewModel>(await responsePoem.Content.ReadAsStringAsync());

            return Page();
        }
    }
}
