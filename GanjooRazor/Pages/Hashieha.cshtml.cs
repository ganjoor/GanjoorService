using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DNTPersianUtils.Core;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Auth.ViewModel;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Services.Implementation;
using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Models.Generic;

namespace GanjooRazor.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class HashiehaModel : LoginPartialEnabledPageModel
    {
        private readonly PoetCacheService _poetCache;

        /// <summary>
        /// constructor
        /// </summary>
        public HashiehaModel(HttpClient httpClient, IConfiguration configuration, PoetCacheService poetCache) : base(httpClient, configuration)
        {
            _poetCache = poetCache;
        }

        public List<GanjoorPoetViewModel> Poets { get; set; }
        public int PoetId { get; set; }
        public GanjoorPoetCompleteViewModel Poet { get; set; }
        public string PagingToolsHtml { get; set; }
        public string LastError { get; set; }

        public List<GanjoorCommentFullViewModel> Comments { get; set; }

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

        public GanjoorUserPublicProfile Profile { get; set; }

        public UserContributionsViewModel Contributions { get; set; }

        public string Title { get; set; }

        public string HomeLink { get; set; }

        public bool CanAdministerUsers { get; set; }

        public string Query { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var maintenanceResult = TryGetMaintenanceModeResult();
            if (maintenanceResult != null)
            {
                return maintenanceResult;
            }

            InitializeCommonPageState();

            PoetId = string.IsNullOrEmpty(Request.Query["a"]) ? 0 : int.Parse(Request.Query["a"]);

            //todo: use html master layout or make it partial
            // 1. poets 
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

            // 2. search comments

            int pageNumber = 1;
            if (!string.IsNullOrEmpty(Request.Query["page"]))
            {
                pageNumber = int.Parse(Request.Query["page"]);
            }

            Query = Request.Query["w"];

            var filterUserId = Request.Query["userid"];
            string url = $"{APIRoot.Url}/api/ganjoor/comments?PageNumber={pageNumber}&PageSize=20";
            Title = "حاشیه‌ها";
            HomeLink = "/hashieha";
            if (!string.IsNullOrEmpty(filterUserId))
            {
                var responseUserProfile = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/user/profile/{filterUserId}");
                if (!responseUserProfile.IsSuccessStatusCode)
                {
                    LastError = await ReadErrorMessageAsync(responseUserProfile);
                    return Page();
                }

                Profile = JsonConvert.DeserializeObject<GanjoorUserPublicProfile>(await responseUserProfile.Content.ReadAsStringAsync());


                var responseContributions = await _httpClient.GetAsync($"{APIRoot.Url}/api/contributions/{filterUserId}");
                if (!responseContributions.IsSuccessStatusCode)
                {
                    LastError = await ReadErrorMessageAsync(responseContributions);
                    return Page();
                }

                Contributions = JsonConvert.DeserializeObject<UserContributionsViewModel>(await responseContributions.Content.ReadAsStringAsync());


                ViewData["Title"] = $"گنجور » حاشیه‌گذاری‌های {Profile.NickName}";

                Title = $"حاشیه‌گذاری‌های {Profile.NickName}";
                HomeLink = $"/hashieha?userid={filterUserId}";

                if (!string.IsNullOrEmpty(Request.Cookies["Token"]))
                {
                    // Was previously a hand-rolled HttpClient with the Bearer token set directly from
                    // the cookie, manually searching /api/users/securableitems for a matching
                    // securable + operation. GanjoorSessionChecker.IsPermitted already does exactly
                    // this and (unlike the hand-rolled version) goes through PrepareClient's session
                    // renewal, so it also correctly handles a token that needs refreshing.
                    CanAdministerUsers = await GanjoorSessionChecker.IsPermitted(Request, Response, SecurableItem.UserEntityShortName, SecurableItem.Administer);
                }

                url += $"&filterUserId={filterUserId}";

            }

            if (!string.IsNullOrEmpty(Query))
            {
                Query = Query.ApplyCorrectYeKe().Trim();
                bool quotes = Query.IndexOf("\"") != -1;
                Query = LanguageUtils.MakeTextSearchable(Query); //replace zwnj with space
                if (quotes)
                    Query = $"\"{Query}\"";
                url += $"&term={Query}";
            }


            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                LastError = await ReadErrorMessageAsync(response);
                return Page();
            }


            Comments = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorCommentFullViewModel>>();

            string paginnationMetadata = response.Headers.GetValues("paging-headers").FirstOrDefault();
            PaginationMetadata paginationMetadata = JsonConvert.DeserializeObject<PaginationMetadata>(paginnationMetadata);

            string htmlText = "";

            if (paginationMetadata.totalPages > 1)
            {
                if (pageNumber > 1)
                    ViewData["Title"] += $" - صفحهٔ {pageNumber.ToPersianNumbers()}";


                htmlText = $"<div>{Environment.NewLine}";
                string queryFilterUserId = string.IsNullOrEmpty(filterUserId) ? "" : $"&amp;userid={filterUserId}";
                if (!string.IsNullOrEmpty(Query))
                {
                    queryFilterUserId += $"&amp;w={WebUtility.UrlEncode(Query)}";
                }
                if (paginationMetadata.currentPage > 3)
                {
                    htmlText += $"<a href=\"/hashieha/?page=1{queryFilterUserId}\"><div class=\"circled-number\">۱</div></a> …";
                }
                for (int i = paginationMetadata.currentPage - 2; i <= (paginationMetadata.currentPage + 2); i++)
                {
                    if (i >= 1 && i <= paginationMetadata.totalPages)
                    {
                        if (i == paginationMetadata.currentPage)
                        {
                            htmlText += $"<div class=\"circled-number-diff\">{i.ToPersianNumbers()}</div>{Environment.NewLine}";
                        }
                        else
                        {
                            htmlText += $"<a href=\"/hashieha/?page={i}{queryFilterUserId}\"><div class=\"circled-number\">{i.ToPersianNumbers()}</div></a>{Environment.NewLine}";
                        }
                    }
                }
                if (paginationMetadata.totalPages > (paginationMetadata.currentPage + 2))
                {
                    htmlText += $"… <a href=\"/hashieha/?page={paginationMetadata.totalPages}{queryFilterUserId}\"><div class=\"circled-number\">{paginationMetadata.totalPages.ToPersianNumbers()}</div></a>{Environment.NewLine}";
                }
                htmlText += $"</div>{Environment.NewLine}";
            }

            ViewData["Title"] = $"گنجور » {Title}";
            PagingToolsHtml = htmlText;

            return Page();
        }
    }
}
