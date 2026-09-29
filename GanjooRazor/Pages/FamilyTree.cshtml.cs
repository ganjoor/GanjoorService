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
    /// interactive family-tree chart, drawn client-side (vanilla JS/SVG - see the &lt;script&gt; block
    /// in FamilyTree.cshtml) from the connected kinship component GET api/people/{id}/familytree
    /// returns. Public/read-only counterpart of Person.cshtml's plain relatives list, reachable from
    /// there and from People.cshtml's family-tree index.
    /// </summary>
    public class FamilyTreeModel : LoginPartialEnabledPageModel
    {
        public FamilyTreeModel(HttpClient httpClient, IConfiguration configuration) : base(httpClient, configuration)
        {
        }

        public string LastError { get; set; }

        public GanjoorRelatedPerson RootPerson { get; set; }

        public GanjoorFamilyTreeViewModel Tree { get; set; }

        /// <summary>
        /// Tree, re-serialized with an explicit camelCase contract (same convention ReviewEdits.cshtml.cs
        /// uses for AllLocationsJson/AllPeopleJson) so the client-side layout script has a predictable
        /// shape to parse, regardless of the API's own default casing
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

            ViewData["Title"] = $"گنجور » شجره‌نامهٔ {(string.IsNullOrEmpty(RootPerson.FamilyTreeCaption) ? RootPerson.Name : RootPerson.FamilyTreeCaption)}";

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
