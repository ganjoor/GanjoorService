using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor.ViewModels;


namespace TajikGanjoor.Pages
{
    public class IndexModel : PageModel
    {
        public List<GanjoorPoetViewModel>? Poets { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (bool.Parse(Configuration["MaintenanceMode"] ?? false.ToString()))
            {
                return StatusCode(503);
            }

            if(false == await PreparePoetsAsync())
            {
                return Page();
            }

            return Page();
        }

        private async Task<bool> PreparePoetsAsync()
        {
            var cacheKey = $"/api/ganjoor/poets";
            if (!_memoryCache.TryGetValue(cacheKey, out List<GanjoorPoetViewModel>? poets))
            {
                try
                {
                    var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poets");
                    if (!response.IsSuccessStatusCode)
                    {
                        LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()) ?? "!response.IsSuccessStatusCode";
                        return false;
                    }
                    poets = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoetViewModel>>();
                    if (AggressiveCacheEnabled)
                    {
                        _memoryCache.Set(cacheKey, poets);
                    }
                }
                catch
                {
                    LastError = "خطا در دسترسی به وب سرویس گنجور";
                    return false;
                }

            }

            Poets = poets ?? [];
            return true;
        }

        public string? LastError { get; set; }

        public bool AggressiveCacheEnabled
        {
            get
            {
                try
                {
                    return bool.Parse(Configuration["AggressiveCacheEnabled"] ?? false.ToString());
                }
                catch
                {
                    return false;
                }
            }
        }


        protected readonly IConfiguration Configuration;
        protected readonly HttpClient _httpClient;
        private readonly IMemoryCache _memoryCache;
        public IndexModel(IConfiguration configuration, HttpClient httpClient, IMemoryCache memoryCache)
        {
            Configuration = configuration;
            _httpClient = httpClient;
            _memoryCache = memoryCache;
        }
    }
}
