using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor.ViewModels;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace GanjooRazor.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class BooksModel : LoginPartialEnabledPageModel
    {
        private readonly PoetCacheService _poetCache;

        public async Task<IActionResult> OnGetAsync()
        {
            var maintenanceResult = TryGetMaintenanceModeResult();
            if (maintenanceResult != null)
            {
                return maintenanceResult;
            }

            ViewData["Title"] = "فهرست کتاب‌ها";
            InitializeCommonPageState();

            var (poetsOk, poets, poetsError) = await _poetCache.GetPoetsAsync(AggressiveCacheEnabled);
            if (!poetsOk)
            {
                LastError = poetsError;
                return Page();
            }
            Poets = poets;

            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/books");
            if (!response.IsSuccessStatusCode)
            {
                LastError = await ReadErrorMessageAsync(response);
                return Page();
            }
            Books = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorCatViewModel>>();
            return Page();
        }

        /// <summary>
        /// books
        /// </summary>
        public List<GanjoorCatViewModel> Books { get; set; }

        /// <summary>
        /// Poets
        /// </summary>
        public List<GanjoorPoetViewModel> Poets { get; set; }

        /// <summary>
        /// last error
        /// </summary>
        public string LastError { get; set; }

        /// <summary>
        /// constructor
        /// </summary>
        public BooksModel(IConfiguration configuration, HttpClient httpClient, PoetCacheService poetCache) : base(httpClient, configuration)
        {
            _poetCache = poetCache;
        }
    }
}

