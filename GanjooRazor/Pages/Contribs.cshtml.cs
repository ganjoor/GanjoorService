using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor.ViewModels;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using RMuseum.Models.Generic.ViewModels;

namespace GanjooRazor.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class ContribsModel : LoginPartialEnabledPageModel
    {
        public List<GanjoorPoetViewModel> Poets { get; set; }
        public int PoetId { get; set; }
        public GanjoorPoetCompleteViewModel Poet { get; set; }
        public string LastError { get; set; }

        /// <summary>
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



        private async Task<bool> preparePoets()
        {
            var cacheKey = $"/api/ganjoor/poets";
            if (!_memoryCache.TryGetValue(cacheKey, out List<GanjoorPoetViewModel> poets))
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
                    _memoryCache.Set(cacheKey, poets);
                }
            }

            Poets = poets;
            return true;
        }

        private async Task<bool> preparePoet()
        {
            var cacheKey = $"/api/ganjoor/poet/{PoetId}";
            if (!_memoryCache.TryGetValue(cacheKey, out GanjoorPoetCompleteViewModel poet))
            {
                var poetResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poet/{PoetId}");
                if (!poetResponse.IsSuccessStatusCode)
                {
                    LastError = JsonConvert.DeserializeObject<string>(await poetResponse.Content.ReadAsStringAsync());
                    return false;
                }
                poet = JObject.Parse(await poetResponse.Content.ReadAsStringAsync()).ToObject<GanjoorPoetCompleteViewModel>();
                if (AggressiveCacheEnabled)
                {
                    _memoryCache.Set(cacheKey, poet);
                }
            }

            Poet = poet;
            return true;
        }

        public async Task<IActionResult> OnGetPoetInformationAsync(int id)
        {
            if (id == 0)
                return new OkObjectResult(null);
            var cacheKey = $"/api/ganjoor/poet/{id}";
            if (!_memoryCache.TryGetValue(cacheKey, out GanjoorPoetCompleteViewModel poet))
            {
                var poetResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poet/{id}");
                if (!poetResponse.IsSuccessStatusCode)
                {
                    return BadRequest(JsonConvert.DeserializeObject<string>(await poetResponse.Content.ReadAsStringAsync()));
                }
                poet = JObject.Parse(await poetResponse.Content.ReadAsStringAsync()).ToObject<GanjoorPoetCompleteViewModel>();
                if (AggressiveCacheEnabled)
                {
                    _memoryCache.Set(cacheKey, poet);
                }
            }
            return new OkObjectResult(poet);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (bool.Parse(Configuration["MaintenanceMode"]))
            {
                return StatusCode(503);
            }

            LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Token"]);

            PoetId = string.IsNullOrEmpty(Request.Query["a"]) ? 0 : int.Parse(Request.Query["a"]);

            ViewData["GoogleAnalyticsCode"] = Configuration["GoogleAnalyticsCode"];

            //todo: use html master layout or make it partial
            // 1. poets 
            if (false == (await preparePoets()))
                return Page();

            if (PoetId != 0)
            {
                if (false == (await preparePoet()))
                    return Page();
            }

           

            return Page();
        }

        public async Task<ActionResult> OnGetGroupedByDateAsync(string dataType)
        {
            var responseDays = await _httpClient.GetAsync($"{APIRoot.Url}/api/contributions/{dataType}/daily?PageNumber=1&PageSize=30");

            if (!responseDays.IsSuccessStatusCode)
            {
                return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await responseDays.Content.ReadAsStringAsync()));
            }
            var days = JArray.Parse(await responseDays.Content.ReadAsStringAsync()).ToObject<List<GroupedByDateViewModel>>();

            List<GroupedByUserViewModel> users = null;

            if(dataType != "users")
            {
                var responseUsers = await _httpClient.GetAsync($"{APIRoot.Url}/api/contributions/{dataType}/by/user?PageNumber=1&PageSize=30");

                if (!responseUsers.IsSuccessStatusCode)
                {
                    return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await responseUsers.Content.ReadAsStringAsync()));
                }
                users = JArray.Parse(await responseUsers.Content.ReadAsStringAsync()).ToObject<List<GroupedByUserViewModel>>();

            }

            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/contributions/{dataType}/summary");

            if (!response.IsSuccessStatusCode)
            {
                return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
            }
            SummedUpViewModel summary = JsonConvert.DeserializeObject<SummedUpViewModel>(await response.Content.ReadAsStringAsync());

            return new PartialViewResult()
            {
                ViewName = "_GroupedByDateViewPartial",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = new _GroupedByDateViewPartialModel()
                    {
                        DataType = dataType,
                        Days = days.ToArray(),
                        Users = users == null ? null :users.ToArray(),
                        Summary = summary
                    }
                }
            };
        }

        /// <summary>
        /// memory cache
        /// </summary>
        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// configration file reader (appsettings.json)
        /// </summary>
        private readonly IConfiguration Configuration;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="memoryCache"></param>
        /// <param name="configuration"></param>
        public ContribsModel(HttpClient httpClient, IMemoryCache memoryCache, IConfiguration configuration) : base(httpClient)
        {
            _memoryCache = memoryCache;
            Configuration
                = configuration;
        }
    }
}
