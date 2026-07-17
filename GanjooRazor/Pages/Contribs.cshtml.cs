using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Models.Generic.ViewModels;
using RSecurityBackend.Models.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace GanjooRazor.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class ContribsModel : LoginPartialEnabledPageModel
    {
        private readonly PoetCacheService _poetCache;

        public List<GanjoorPoetViewModel> Poets { get; set; }
        public int PoetId { get; set; }
        public GanjoorPoetCompleteViewModel Poet { get; set; }
        public string LastError { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var maintenanceResult = TryGetMaintenanceModeResult();
            if (maintenanceResult != null)
            {
                return maintenanceResult;
            }

            InitializeCommonPageState();
            PoetId = string.IsNullOrEmpty(Request.Query["a"]) ? 0 : int.Parse(Request.Query["a"]);

            var (poetsOk, poets, poetsError) = await _poetCache.GetPoetsAsync(AggressiveCacheEnabled);
            if (!poetsOk)
            {
                LastError = poetsError;
                return Page();
            }
            Poets = poets;

            if (PoetId != 0)
            {
                var (poetOk, poet, poetError) = await _poetCache.GetPoetAsync(PoetId, AggressiveCacheEnabled);
                if (!poetOk)
                {
                    LastError = poetError;
                    return Page();
                }
                Poet = poet;
            }

            return Page();
        }

        public async Task<IActionResult> OnGetPoetInformationAsync(int id)
        {
            if (id == 0)
            {
                return new OkObjectResult(null);
            }
            var (success, poet, error) = await _poetCache.GetPoetAsync(id, AggressiveCacheEnabled);
            if (!success)
            {
                return BadRequest(error);
            }
            return new OkObjectResult(poet);
        }

        public async Task<IActionResult> OnGetGroupedByDateAsync(string dataType)
        {
            var responseDays = await _httpClient.GetAsync($"{APIRoot.Url}/api/contributions/{dataType}/daily?PageNumber=1&PageSize=30");
            if (!responseDays.IsSuccessStatusCode)
            {
                return new BadRequestObjectResult(await ReadErrorMessageAsync(responseDays));
            }
            var days = JArray.Parse(await responseDays.Content.ReadAsStringAsync()).ToObject<List<GroupedByDateViewModel>>();

            List<GroupedByUserViewModel> users = null;
            PaginationMetadata usersPagination = null;
            if (dataType != "users")
            {
                var responseUsers = await _httpClient.GetAsync($"{APIRoot.Url}/api/contributions/{dataType}/by/user?PageNumber=1&PageSize=30");
                if (!responseUsers.IsSuccessStatusCode)
                {
                    return new BadRequestObjectResult(await ReadErrorMessageAsync(responseUsers));
                }
                users = JArray.Parse(await responseUsers.Content.ReadAsStringAsync()).ToObject<List<GroupedByUserViewModel>>();
                usersPagination = JsonConvert.DeserializeObject<PaginationMetadata>(responseUsers.Headers.GetValues("paging-headers").Single());
            }

            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/contributions/{dataType}/summary");
            if (!response.IsSuccessStatusCode)
            {
                return new BadRequestObjectResult(await ReadErrorMessageAsync(response));
            }
            SummedUpViewModel summary = JsonConvert.DeserializeObject<SummedUpViewModel>(await response.Content.ReadAsStringAsync());

            return Partial("~/Pages/Partials/Contribs/_GroupedByDateViewPartial.cshtml", new _GroupedByDateViewPartialModel()
            {
                DataType = dataType,
                Days = days.ToArray(),
                DaysPagination = JsonConvert.DeserializeObject<PaginationMetadata>(responseDays.Headers.GetValues("paging-headers").Single()),
                Users = users?.ToArray(),
                UsersPagination = usersPagination,
                Summary = summary
            });
        }

        public async Task<IActionResult> OnGetGroupedByUsersAsync(string dataType, int pageNumber)
        {
            var responseUsers = await _httpClient.GetAsync($"{APIRoot.Url}/api/contributions/{dataType}/by/user?PageNumber={pageNumber}&PageSize=30");
            if (!responseUsers.IsSuccessStatusCode)
            {
                return new BadRequestObjectResult(await ReadErrorMessageAsync(responseUsers));
            }
            var users = JArray.Parse(await responseUsers.Content.ReadAsStringAsync()).ToObject<List<GroupedByUserViewModel>>();
            var usersPagination = JsonConvert.DeserializeObject<PaginationMetadata>(responseUsers.Headers.GetValues("paging-headers").Single());

            return Partial("~/Pages/Partials/Contribs/_GroupedByDateViewTablePartial.cshtml", new _GroupedByDateViewTablePartialModel()
            {
                DataType = dataType,
                Users = users.ToArray(),
                UsersPagination = usersPagination,
            });
        }

        public async Task<IActionResult> OnGetGroupedByDayAsync(string dataType, int pageNumber)
        {
            var responseDays = await _httpClient.GetAsync($"{APIRoot.Url}/api/contributions/{dataType}/daily?PageNumber={pageNumber}&PageSize=30");
            if (!responseDays.IsSuccessStatusCode)
            {
                return new BadRequestObjectResult(await ReadErrorMessageAsync(responseDays));
            }
            var days = JArray.Parse(await responseDays.Content.ReadAsStringAsync()).ToObject<List<GroupedByDateViewModel>>();

            return Partial("~/Pages/Partials/Contribs/_GroupedByDateViewTablePartial.cshtml", new _GroupedByDateViewTablePartialModel()
            {
                DataType = dataType,
                Days = days.ToArray(),
                DaysPagination = JsonConvert.DeserializeObject<PaginationMetadata>(responseDays.Headers.GetValues("paging-headers").Single()),
            });
        }

        /// <summary>
        /// constructor
        /// </summary>
        public ContribsModel(HttpClient httpClient, IConfiguration configuration, PoetCacheService poetCache) : base(httpClient, configuration)
        {
            _poetCache = poetCache;
        }
    }
}
