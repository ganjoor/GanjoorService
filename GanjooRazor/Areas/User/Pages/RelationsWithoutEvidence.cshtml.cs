using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GanjooRazor.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;

namespace GanjooRazor.Areas.User.Pages
{
    /// <summary>
    /// worklist of kinship relations that have no human-attached evidence couplet yet, each with a
    /// link to the evidence suggestion form (SuggestPersonRelationEdit). Any logged-in user may use it.
    /// </summary>
    public class RelationsWithoutEvidenceModel : LoginPartialEnabledPageModel
    {
        public RelationsWithoutEvidenceModel(HttpClient httpClient, IConfiguration configuration) : base(httpClient, configuration)
        {
        }

        public int PageSize { get; } = 50;

        public string LastError { get; set; }

        public GanjoorRelationWithoutEvidence[] Rows { get; set; }

        public int TotalCount { get; set; }

        public int Skip { get; set; }

        public int? PersonId { get; set; }

        public async Task<IActionResult> OnGetAsync(int skip = 0, int? personId = null)
        {
            InitializeCommonPageState();

            if (!LoggedIn)
            {
                return Redirect($"/login?redirect={Uri.EscapeDataString(Request.Path + Request.QueryString)}");
            }

            Skip = Math.Max(skip, 0);
            PersonId = personId;

            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/people/relations/withoutevidence?skip={Skip}&take={PageSize}" + (personId.HasValue ? $"&personId={personId.Value}" : ""));
            if (!response.IsSuccessStatusCode)
            {
                LastError = await ReadErrorMessageAsync(response);
                return Page();
            }

            var header = response.Headers.GetValues("paging-headers").FirstOrDefault();
            if (!string.IsNullOrEmpty(header))
            {
                TotalCount = JsonConvert.DeserializeObject<PaginationMetadata>(header).totalCount;
            }
            Rows = JsonConvert.DeserializeObject<GanjoorRelationWithoutEvidence[]>(await response.Content.ReadAsStringAsync());

            return Page();
        }
    }
}
