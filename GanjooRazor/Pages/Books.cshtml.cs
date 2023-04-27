using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor.ViewModels;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace GanjooRazor.Pages
{
    public class BooksModel : PageModel
    {
        public async Task<IActionResult> OnGetAsync()
        {
            if (bool.Parse(Configuration["MaintenanceMode"]))
            {
                return StatusCode(503);
            }
            ViewData["Title"] = "›Â—”  ò «»ùÂ«";

            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/books");
            if (!response.IsSuccessStatusCode)
            {
                LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                return Page();
            }
            Books = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorCatViewModel>>();
            return Page();
        }

        /// <summary>
        /// configration file reader (appsettings.json)
        /// </summary>
        protected readonly IConfiguration Configuration;

        /// <summary>
        /// HttpClient instance
        /// </summary>
        protected readonly HttpClient _httpClient;

        /// <summary>
        /// books
        /// </summary>
        public List<GanjoorCatViewModel>   Books { get; set; }

        /// <summary>
        /// last error
        /// </summary>
        public string LastError { get; set; }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="httpClient"></param>
        public BooksModel(IConfiguration configuration,
           HttpClient httpClient
           ) 
        {
            Configuration = configuration;
            _httpClient = httpClient;
            
        }
    }
}
