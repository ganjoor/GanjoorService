using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor.ViewModels;

namespace GanjooRazor.Areas.Admin.Pages
{
    public class CatUtilsModel : PageModel
    {
        // <summary>
        /// HttpClient instance
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// memory cache
        /// </summary>
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="memoryCache"></param>
        public CatUtilsModel(HttpClient httpClient, IMemoryCache memoryCache)
        {
            _httpClient = httpClient;
            _memoryCache = memoryCache;
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
        /// last message
        /// </summary>
        public string LastMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            LastMessage = Request.Query["edit"] == "true" ? "ویرایش انجام شد." : "";
            if (string.IsNullOrEmpty(Request.Query["url"]))
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "نشانی صفحه مشخص نیست.");
            }

            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/cat?url={Request.Query["url"]}&poems=true");

            response.EnsureSuccessStatusCode();

            Cat = JsonConvert.DeserializeObject<GanjoorPoetCompleteViewModel>(await response.Content.ReadAsStringAsync());

            var pageQuery = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/page?url={Request.Query["url"]}");
            if (!pageQuery.IsSuccessStatusCode)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, await pageQuery.Content.ReadAsStringAsync());
            }
            PageInformation = JObject.Parse(await pageQuery.Content.ReadAsStringAsync()).ToObject<GanjoorPageCompleteViewModel>();


            NamingModel = new GanjoorBatchNamingModel()
            {
                StartWithNotIncludingSpaces = "شمارهٔ ",
                RemovePreviousPattern = true,
                RemoveSetOfCharacters = ".-"
            };

            return Page();
        }
    }
}
