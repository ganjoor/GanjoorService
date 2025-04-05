using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Models.GanjoorAudio.ViewModels;

namespace GanjooRazor.Pages
{
    public class AudioClipModel : PageModel
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
        public AudioClipModel(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            Configuration = configuration;
        }

        /// <summary>
        /// recitation
        /// </summary>
        public PublicRecitationViewModel Recitation { get; set; }

        /// <summary>
        /// poem
        /// </summary>
        public GanjoorPoemCompleteViewModel Poem { get; set; }

        public string LastError { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (bool.Parse(Configuration["MaintenanceMode"]))
            {
                return StatusCode(503);
            }

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            ViewData["TrackingScript"] = Configuration["TrackingScript"];

            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/audio/published/{Request.Query["a"]}");
            if (!response.IsSuccessStatusCode)
            {
                LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                return Page();
            }

            Recitation = JsonConvert.DeserializeObject<PublicRecitationViewModel>(await response.Content.ReadAsStringAsync());

            var responsePoem = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poem/{Recitation.PoemId}?verseDetails=true&catInfo=true&rhymes=false&recitations=false&images=false&songs=false&comments=false&navigation=false");
            if (!responsePoem.IsSuccessStatusCode)
            {
                LastError = JsonConvert.DeserializeObject<string>(await responsePoem.Content.ReadAsStringAsync());
                return Page();
            }

            Poem = JsonConvert.DeserializeObject<GanjoorPoemCompleteViewModel>(await responsePoem.Content.ReadAsStringAsync());

            return Page();

        }
    }
}
