using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;

namespace GanjooRazor.Pages
{
    /// <summary>
    /// interactive family-tree chart, drawn client-side (vanilla JS/SVG - see familytree.js) from
    /// the connected kinship component GET api/people/{id}/familytree returns. Inline-modal
    /// counterpart of the old standalone FamilyTree.cshtml page (now removed) - fetched as a
    /// fragment (Layout = null, no site chrome) and opened over whatever page the user is already
    /// on via FamilyTreeWindow.open(id) in personwindow.js, the same pattern PersonWindow.cshtml/
    /// PeopleExplorer.cshtml use.
    /// </summary>
    public class FamilyTreeWindowModel : LoginPartialEnabledPageModel
    {
        public FamilyTreeWindowModel(HttpClient httpClient, IConfiguration configuration) : base(httpClient, configuration)
        {
        }

        public string LastError { get; set; }

        public GanjoorRelatedPerson RootPerson { get; set; }

        public GanjoorFamilyTreeViewModel Tree { get; set; }

        /// <summary>
        /// Tree, re-serialized with an explicit camelCase contract (same convention
        /// _PersonGraphPartial.cshtml.cs's GraphDataJson uses) so familytree.js has a predictable
        /// shape to parse once this fragment's data block is read, regardless of the API's own
        /// default casing
        /// </summary>
        public string TreeDataJson =>
            JsonConvert.SerializeObject(
                Tree,
                new JsonSerializerSettings { ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver() }
            );

        public async Task<IActionResult> OnGetAsync(int id)
        {
            InitializeCommonPageState();

            var personResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/people/{id}");
            if (!personResponse.IsSuccessStatusCode)
            {
                LastError = await ReadErrorMessageAsync(personResponse);
                return Page();
            }
            RootPerson = JsonConvert.DeserializeObject<GanjoorRelatedPerson>(await personResponse.Content.ReadAsStringAsync());
            if (RootPerson == null)
            {
                LastError = "شخصیتی با این کد پیدا نشد.";
                return Page();
            }

            var treeResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/people/{id}/familytree");
            if (!treeResponse.IsSuccessStatusCode)
            {
                LastError = await ReadErrorMessageAsync(treeResponse);
                return Page();
            }
            Tree = JsonConvert.DeserializeObject<GanjoorFamilyTreeViewModel>(await treeResponse.Content.ReadAsStringAsync());

            return Page();
        }
    }
}
