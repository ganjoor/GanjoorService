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
    /// "who could have been alive at the same time" modal - fetches the person's kinship/affiliation
    /// component from GET api/people/{id}/contemporaries and hands it to contemporaries.js, which does
    /// the timing reasoning client-side (see that file's header). Bare fragment (Layout = null), opened
    /// via ContemporariesWindow.open(id) in personwindow.js - same pattern as FamilyTreeWindow.
    /// </summary>
    public class ContemporariesWindowModel : LoginPartialEnabledPageModel
    {
        public ContemporariesWindowModel(HttpClient httpClient, IConfiguration configuration) : base(httpClient, configuration)
        {
        }

        public string LastError { get; set; }

        public GanjoorRelatedPerson RootPerson { get; set; }

        public GanjoorContemporaryGraphViewModel Graph { get; set; }

        /// <summary>
        /// Graph with an explicit camelCase contract, so contemporaries.js has a predictable shape
        /// </summary>
        public string GraphDataJson =>
            JsonConvert.SerializeObject(
                new
                {
                    rootId = Graph.RootId,
                    persons = Graph.Persons,
                    kin = Graph.Kin,
                    affiliations = Graph.Affiliations
                },
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

            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/people/{id}/contemporaries");
            if (!response.IsSuccessStatusCode)
            {
                LastError = await ReadErrorMessageAsync(response);
                return Page();
            }
            Graph = JsonConvert.DeserializeObject<GanjoorContemporaryGraphViewModel>(await response.Content.ReadAsStringAsync());

            return Page();
        }
    }
}
