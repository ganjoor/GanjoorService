using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
        /// constructor
        /// </summary>
        /// <param name="httpClient"></param>
        public PoetModel(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }


        [BindProperty]
        public GanjoorPoetViewModel Poet { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poet/{Request.Query["id"]}");

            response.EnsureSuccessStatusCode();

            var poet = JsonConvert.DeserializeObject<GanjoorPoetCompleteViewModel>(await response.Content.ReadAsStringAsync());


            Poet = poet.Poet;


            return Page();
        }
    }
}
