using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;

namespace GanjooRazor.Areas.Admin.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class CatUtilsModel : PageModel
    {
        // <summary>
        /// HttpClient instance
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="memoryCache"></param>
        public CatUtilsModel(HttpClient httpClient, IMemoryCache memoryCache)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// category
        /// </summary>
        public GanjoorPoetCompleteViewModel Cat { get; set; }

        /// <summary>
        /// cat page
        /// </summary>
        public GanjoorPageCompleteViewModel PageInformation { get; set; }

        [BindProperty]
        public GanjoorBatchNamingModel NamingModel { get; set; }


        /// <summary>
        /// rythm
        /// </summary>
        public GanjoorMetre[] Rhythms { get; set; }


        /// <summary>
        /// last message
        /// </summary>
        public string LastMessage { get; set; }

        /// <summary>
        /// renaming output
        /// </summary>
        public string[] RenamingOutput { get; set; }

        /// <summary>
        /// numbering patterns
        /// </summary>
        public GanjoorNumbering[] Numberings { get; set; }

        private async Task GetInformationAsync()
        {
           

            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/cat?url={Request.Query["url"]}&poems=true");
            response.EnsureSuccessStatusCode();
            Cat = JsonConvert.DeserializeObject<GanjoorPoetCompleteViewModel>(await response.Content.ReadAsStringAsync());

            var pageQuery = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/page?url={Request.Query["url"]}");
            pageQuery.EnsureSuccessStatusCode();
            PageInformation = JObject.Parse(await pageQuery.Content.ReadAsStringAsync()).ToObject<GanjoorPageCompleteViewModel>();

            var rhythmsResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/rhythms");
            rhythmsResponse.EnsureSuccessStatusCode();
            Rhythms = JsonConvert.DeserializeObject<GanjoorMetre[]>(await rhythmsResponse.Content.ReadAsStringAsync());

            var numberings = await _httpClient.GetAsync($"{APIRoot.Url}/api/numberings/cat/{Cat.Cat.Id}");
            numberings.EnsureSuccessStatusCode();
            Numberings = JsonConvert.DeserializeObject<GanjoorNumbering[]>(await numberings.Content.ReadAsStringAsync());
        }

        public async Task<IActionResult> OnGetAsync()
        {
            LastMessage = Request.Query["edit"] == "true" ? "ویرایش انجام شد." : "";

            if (string.IsNullOrEmpty(Request.Query["url"]))
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "نشانی صفحه مشخص نیست.");
            }

            await GetInformationAsync();

            NamingModel = new GanjoorBatchNamingModel()
            {
                StartWithNotIncludingSpaces = "شمارهٔ ",
                RemovePreviousPattern = true,
                RemoveSetOfCharacters = ".-",
                Simulate = true
            };

            return Page();
        }

        /// <summary>
        /// تغییر عنوان گروهی
        /// </summary>
        /// <param name="NamingModel"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostAsync(GanjoorBatchNamingModel NamingModel)
        {
            await GetInformationAsync();

            using (HttpClient secureClient = new HttpClient())
            {
                await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response);

                HttpResponseMessage response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/cat/recaptionpoems/{Cat.Cat.Id}", new StringContent(JsonConvert.SerializeObject(NamingModel), Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();

                RenamingOutput = JsonConvert.DeserializeObject<string[]>(await response.Content.ReadAsStringAsync());

                NamingModel.Simulate = false;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostStartRhymeAnalysisAsync(int id)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/cat/startassigningrhymes/{id}/{false}", null);
                    response.EnsureSuccessStatusCode();
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }

        public async Task<IActionResult> OnPostStartGeneratingSubCatsTOCAsync(int id)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/cat/subcats/startgentoc/{id}", null);
                    response.EnsureSuccessStatusCode();
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }

        public async Task<IActionResult> OnPostStartRhythmAnalysisAsync(int id, string rhythm)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    HttpResponseMessage response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/cat/startassigningrhythms/{id}/{false}?rhythm={rhythm}", null);
                    response.EnsureSuccessStatusCode();
                    return new OkObjectResult(true);
                }
            }
            return new OkObjectResult(false);
        }

        
    }
}
