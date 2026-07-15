using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor.ViewModels;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace GanjooRazor.Utils
{
    /// <summary>
    /// Fetches the poet list and individual poet details from the Ganjoor API, optionally caching
    /// the results in-memory. This logic (fetch-or-return-cached, deserialize, cache if enabled) was
    /// previously copy-pasted as three near-identical private methods (a "get all poets" fetcher, a
    /// "get one poet by id and store on Model.Poet" fetcher, and an "get one poet by id and return as
    /// JSON" AJAX handler) in IndexModel, ContribsModel, HashiehaModel, SearchModel, and SimiModel.
    ///
    /// Registered as a scoped service in Startup.cs; inject via constructor like any other service.
    /// </summary>
    public class PoetCacheService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _memoryCache;

        public PoetCacheService(HttpClient httpClient, IMemoryCache memoryCache)
        {
            _httpClient = httpClient;
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// Gets the full poet list. Returns (true, poets, null) on success, or (false, null, error)
        /// on failure - callers decide what to do with the error (set LastError and return Page(),
        /// return BadRequest(error), etc.) since that varies per caller.
        /// </summary>
        public async Task<(bool success, List<GanjoorPoetViewModel> poets, string error)> GetPoetsAsync(bool cacheResult)
        {
            const string cacheKey = "/api/ganjoor/poets";
            if (_memoryCache.TryGetValue(cacheKey, out List<GanjoorPoetViewModel> poets))
            {
                return (true, poets, null);
            }

            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poets");
            if (!response.IsSuccessStatusCode)
            {
                return (false, null, JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
            }

            poets = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoetViewModel>>();
            if (cacheResult)
            {
                _memoryCache.Set(cacheKey, poets, TimeSpan.FromHours(1));
            }
            return (true, poets, null);
        }

        /// <summary>
        /// Gets a single poet's full details by id. Returns (true, poet, null) on success, or
        /// (false, null, error) on failure.
        /// </summary>
        public async Task<(bool success, GanjoorPoetCompleteViewModel poet, string error)> GetPoetAsync(int poetId, bool cacheResult)
        {
            var cacheKey = $"/api/ganjoor/poet/{poetId}";
            if (_memoryCache.TryGetValue(cacheKey, out GanjoorPoetCompleteViewModel poet))
            {
                return (true, poet, null);
            }

            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poet/{poetId}");
            if (!response.IsSuccessStatusCode)
            {
                return (false, null, JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
            }

            poet = JObject.Parse(await response.Content.ReadAsStringAsync()).ToObject<GanjoorPoetCompleteViewModel>();
            if (cacheResult)
            {
                _memoryCache.Set(cacheKey, poet, TimeSpan.FromHours(1));
            }
            return (true, poet, null);
        }
    }
}
