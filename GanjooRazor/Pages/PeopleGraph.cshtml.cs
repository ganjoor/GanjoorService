using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor.ViewModels;

namespace GanjooRazor.Pages
{
    /// <summary>
    /// force-directed "ontology" explorer over the whole known people network (kinship edges and
    /// non-family ties together), drawn client-side (vanilla JS/SVG - see the &lt;script&gt; block in
    /// PeopleGraph.cshtml) from what GET api/people/graph returns. The nowruzgan-style counterpart of
    /// the strict-tree /FamilyTree/{id} view: not limited to one family, shows every recorded
    /// relationship at once with a synced data table and click-to-focus ego network.
    /// </summary>
    public class PeopleGraphModel : LoginPartialEnabledPageModel
    {
        public PeopleGraphModel(HttpClient httpClient, IConfiguration configuration) : base(httpClient, configuration)
        {
        }

        public string LastError { get; set; }

        public GanjoorPersonGraphViewModel Graph { get; set; }

        /// <summary>
        /// Graph, re-serialized with an explicit camelCase contract (same convention FamilyTree.cshtml.cs
        /// uses for TreeDataJson) so the client-side layout script has a predictable shape to parse
        /// </summary>
        public string GraphDataJson =>
            JsonConvert.SerializeObject(
                Graph,
                new JsonSerializerSettings { ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver() }
            );

        public async Task<IActionResult> OnGetAsync()
        {
            InitializeCommonPageState();

            ViewData["Title"] = "گنجور » شبکهٔ روابط شخصیت‌ها";

            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/people/graph");
            if (!response.IsSuccessStatusCode)
            {
                LastError = await ReadErrorMessageAsync(response);
                return Page();
            }
            Graph = JsonConvert.DeserializeObject<GanjoorPersonGraphViewModel>(await response.Content.ReadAsStringAsync());

            return Page();
        }
    }
}
