using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RMuseum.Models.GanjoorAudio.ViewModels;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace GanjooRazor.Pages
{
    public class RecitationsOrderModel : PageModel
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
        public RecitationsOrderModel(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            Configuration = configuration;
        }

        /// <summary>
        /// Last Error
        /// </summary>
        public string LastError { get; set; }

        public RecitationOrderingViewModel[] Scores { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (bool.Parse(Configuration["MaintenanceMode"]))
            {
                return StatusCode(503);
            }

            LastError = "";

            if (!string.IsNullOrEmpty(Request.Query["p"]))
            {
                var poemId = int.Parse(Request.Query["p"]);
                var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/audio/votes/{poemId}/scores");
                if (!response.IsSuccessStatusCode)
                {
                    LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                }
                else
                {
                    Scores = JsonConvert.DeserializeObject<RecitationOrderingViewModel[]>(await response.Content.ReadAsStringAsync());
                    if(Scores.Length > 0)
                    {
                        response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poem/{poemId}/recitations");
                        if(!response.IsSuccessStatusCode)
                        {
                            LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                        }
                        var recitations = JsonConvert.DeserializeObject<PublicRecitationViewModel[]>(await response.Content.ReadAsStringAsync());
                        foreach (var score in Scores)
                        {
                            score.Recitation = recitations.Where(r => r.Id == score.RecitationId).Single();
                        }
                    }
                }
            }
            else
            {
                LastError = "شعری انتخاب نشده است.";
            }

            return Page();
        }
    }
}
