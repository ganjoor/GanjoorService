using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor.ViewModels;
using Microsoft.Extensions.Configuration;

namespace GanjooRazor.Pages
{
    public class t6eModel : PageModel
    {
        /// <summary>
        /// poem
        /// </summary>
        public GanjoorPoemCompleteViewModel Poem { get; set; }

        /// <summary>
        /// last error
        /// </summary>
        public string LastError { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (bool.Parse(Configuration["MaintenanceMode"]))
            {
                return StatusCode(503);
            }

            var responsePoem = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poem/{Request.Query["p"]}?verseDetails=true&catInfo=false&rhymes=false&recitations=false&images=false&songs=false&comments=false&navigation=false");
            if (!responsePoem.IsSuccessStatusCode)
            {
                LastError = JsonConvert.DeserializeObject<string>(await responsePoem.Content.ReadAsStringAsync());
                return Page();
            }

            Poem = JsonConvert.DeserializeObject<GanjoorPoemCompleteViewModel>(await responsePoem.Content.ReadAsStringAsync());
            return Page();
        }

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
        public t6eModel(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            Configuration
                = configuration;
        }
    }
}
