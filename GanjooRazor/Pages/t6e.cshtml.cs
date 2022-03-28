using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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
        /// constructor
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="configuration"></param>
        public t6eModel(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
    }
}
