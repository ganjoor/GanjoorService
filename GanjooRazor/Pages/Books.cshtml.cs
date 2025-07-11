using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor.ViewModels;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using System;

namespace GanjooRazor.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class BooksModel : LoginPartialEnabledPageModel
    {
        public async Task<IActionResult> OnGetAsync()
        {
            if (bool.Parse(Configuration["MaintenanceMode"]))
            {
                return StatusCode(503);
            }
            ViewData["Title"] = "فهرست کتاب‌ها";
            ViewData["TrackingScript"] = Configuration["TrackingScript"] != null && string.IsNullOrEmpty(Request.Cookies["Token"]) ? Configuration["TrackingScript"].Replace("loggedon", "") : Configuration["TrackingScript"];
            LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Token"]);

            if (false == await preparePoets())
            {
                return Page();
            }

            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/books");
            if (!response.IsSuccessStatusCode)
            {
                LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                return Page();
            }
            Books = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorCatViewModel>>();
            return Page();
        }

        private async Task<bool> preparePoets()
        {
            var cacheKey = $"/api/ganjoor/poets";
            if (!_memoryCache.TryGetValue(cacheKey, out List<GanjoorPoetViewModel> poets))
            {
                try
                {
                    var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poets");
                    if (!response.IsSuccessStatusCode)
                    {
                        LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                        return false;
                    }
                    poets = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoetViewModel>>();
                    if (AggressiveCacheEnabled)
                    {
                        _memoryCache.Set(cacheKey, poets, TimeSpan.FromHours(1));
                    }
                }
                catch
                {
                    LastError = "خطا در دسترسی به وب سرویس گنجور";
                    return false;
                }

            }

            Poets = poets;
            return true;
        }

        /// <summary>
        /// configration file reader (appsettings.json)
        /// </summary>
        protected readonly IConfiguration Configuration;

        /// <summary>
        /// books
        /// </summary>
        public List<GanjoorCatViewModel>   Books { get; set; }

        /// <summary>
        /// Poets
        /// </summary>
        public List<GanjoorPoetViewModel> Poets { get; set; }

        /// <summary>
        /// last error
        /// </summary>
        public string LastError { get; set; }

        /// <summary>
        /// memory cache
        /// </summary>
        private readonly IMemoryCache _memoryCache;

        // <summary>
        /// aggressive cache
        /// </summary>
        public bool AggressiveCacheEnabled
        {
            get
            {
                try
                {
                    return bool.Parse(Configuration["AggressiveCacheEnabled"]);
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="httpClient"></param>
        /// <param name="memoryCache"></param>
        public BooksModel(IConfiguration configuration,
           HttpClient httpClient, IMemoryCache memoryCache
           ) : base(httpClient)
        {
            Configuration = configuration;
            _memoryCache = memoryCache;
        }
    }
}
