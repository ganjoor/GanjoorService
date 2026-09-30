using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;

namespace GanjooRazor.Pages
{
    /// <summary>
    /// the "explore all characters" modal - the whole-site force-directed relationship graph
    /// (formerly the standalone page PeopleGraph.cshtml) plus the family-tree-roots index
    /// (formerly the standalone page People.cshtml), merged into one bare fragment (Layout = null)
    /// and opened via PeopleExplorer.open() in personwindow.js. Neither of those two pages had an
    /// entry point in the site's own navigation, so folding them together here loses nothing -
    /// it's reachable from inside PersonWindow and from the category-scoped "شخصیت‌ها" tab's
    /// "نمایش شبکهٔ کامل شخصیت‌ها" link.
    /// </summary>
    public class PeopleExplorerModel : LoginPartialEnabledPageModel
    {
        public PeopleExplorerModel(HttpClient httpClient, IConfiguration configuration) : base(httpClient, configuration)
        {
        }

        public string LastError { get; set; }

        public List<GanjoorRelatedPerson> FamilyTreeRoots { get; set; }

        public GanjoorPersonGraphViewModel Graph { get; set; }

        /// <summary>
        /// Graph, re-serialized with an explicit camelCase contract (same convention
        /// PeopleGraphModel/_PersonGraphPartialModel used) so peoplegraph.js has a predictable
        /// shape to parse
        /// </summary>
        public string GraphDataJson =>
            JsonConvert.SerializeObject(
                Graph,
                new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() }
            );

        public async Task<IActionResult> OnGetAsync()
        {
            InitializeCommonPageState();

            var treesResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/people/familytrees");
            if (!treesResponse.IsSuccessStatusCode)
            {
                LastError = await ReadErrorMessageAsync(treesResponse);
                return Page();
            }
            FamilyTreeRoots = JsonConvert.DeserializeObject<List<GanjoorRelatedPerson>>(await treesResponse.Content.ReadAsStringAsync());

            var graphResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/people/graph");
            if (!graphResponse.IsSuccessStatusCode)
            {
                LastError = await ReadErrorMessageAsync(graphResponse);
                return Page();
            }
            Graph = JsonConvert.DeserializeObject<GanjoorPersonGraphViewModel>(await graphResponse.Content.ReadAsStringAsync());

            return Page();
        }
    }
}
