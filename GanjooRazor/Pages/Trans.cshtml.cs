using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RMuseum.Models.Auth.Memory;
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
        private readonly IConfiguration Configuration;


        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="configuration"></param>
        public TransModel(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            Configuration = configuration;
        }
        /// <summary>
        /// poem
        /// </summary>
        public GanjoorPoemCompleteViewModel Poem { get; set; }

        public GanjoorLanguage[] Languages { get; set; }

        public GanjoorPoemTranslationViewModel[] Translations { get; set; }

        public GanjoorPoemTranslationViewModel Translation { get; set; }

        public string ErrorMessage { get; set; }

        public int PoemId { get; set; }

        /// <summary>
        /// is logged on
        /// </summary>
        public bool CanTranslate { get; set; }

        public int LanguageId { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (bool.Parse(Configuration["MaintenanceMode"]))
            {
                return StatusCode(503);
            }

            ErrorMessage = "";

            CanTranslate = false;
            if (!string.IsNullOrEmpty(Request.Cookies["Token"]))
            {
                await GanjoorSessionChecker.ApplyPermissionsToViewData(Request, Response, ViewData);
                if (ViewData.ContainsKey($"{RMuseumSecurableItem.GanjoorEntityShortName}-{RMuseumSecurableItem.Translations}"))
                {
                    CanTranslate = true;
                }
            }

            

            PoemId = int.Parse(Request.Query["p"]);

            ViewData["GoogleAnalyticsCode"] = Configuration["GoogleAnalyticsCode"];

            HttpResponseMessage responseLanguages = await _httpClient.GetAsync($"{APIRoot.Url}/api/translations/languages");
            if (!responseLanguages.IsSuccessStatusCode)
            {
                ErrorMessage = JsonConvert.DeserializeObject<string>(await responseLanguages.Content.ReadAsStringAsync());
                return Page();
            }

            var allLanguages = JsonConvert.DeserializeObject<GanjoorLanguage[]>(await responseLanguages.Content.ReadAsStringAsync());

            HttpResponseMessage response = await _httpClient.GetAsync($"{APIRoot.Url}/api/translations/poem/{PoemId}/published");
            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                return Page();
            }

            Translations = JsonConvert.DeserializeObject<GanjoorPoemTranslationViewModel[]>(await response.Content.ReadAsStringAsync());

            LanguageId = string.IsNullOrEmpty(Request.Query["lang"]) ? -1 : int.Parse(Request.Query["lang"]);

            List<GanjoorLanguage> poemLanguages = new List<GanjoorLanguage>();
            foreach (var lang in allLanguages)
            {
                if (Translations.Where(t => t.Language.Id == lang.Id).FirstOrDefault() != null)
                {
                    poemLanguages.Add(lang);
                }
            }
            if (LanguageId != -1)
                if (!poemLanguages.Where(l => l.Id == LanguageId).Any())
                {
                    poemLanguages.Add(allLanguages.Where(l => l.Id == LanguageId).Single());
                }
            Languages = poemLanguages.ToArray();

            if (LanguageId != -1)
                Translations = Translations.Where(t => t.Language.Id == LanguageId).ToArray();

            Translation = Translations.Length > 0 ? Translations[0] : null;

            if (Translation != null)
                LanguageId = Translation.Language.Id;
            var responsePoem = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poem/{PoemId}?verseDetails=true&catInfo=true&rhymes=false&recitations=false&images=false&songs=false&comments=false&navigation=true");
            if (!responsePoem.IsSuccessStatusCode)
            {
                ErrorMessage = JsonConvert.DeserializeObject<string>(await responsePoem.Content.ReadAsStringAsync());
                return Page();
            }
            Poem = JsonConvert.DeserializeObject<GanjoorPoemCompleteViewModel>(await responsePoem.Content.ReadAsStringAsync());
            return Page();
        }
    }
}
