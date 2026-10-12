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
    /// "presence chart" modal: book sections as columns, persons as rows, a person's span running from the
    /// first to the last section where he/she is tagged. Data comes from GET api/people/{id}/presence and
    /// is drawn by contemporaries.js. Bare fragment (Layout = null), opened via ContemporariesWindow.open(id, masterCatId).
    /// </summary>
    public class ContemporariesWindowModel : LoginPartialEnabledPageModel
    {
        public ContemporariesWindowModel(HttpClient httpClient, IConfiguration configuration) : base(httpClient, configuration)
        {
        }

        public string LastError { get; set; }

        public GanjoorRelatedPerson RootPerson { get; set; }

        public GanjoorPresenceTimelineViewModel Timeline { get; set; }

        public string TimelineJson =>
            JsonConvert.SerializeObject(
                Timeline,
                new JsonSerializerSettings { ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver() }
            );

        public async Task<IActionResult> OnGetAsync(int id, int? masterCatId = null)
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

            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/people/{id}/presence" + (masterCatId != null ? $"?masterCatId={masterCatId}" : ""));
            if (!response.IsSuccessStatusCode)
            {
                LastError = await ReadErrorMessageAsync(response);
                return Page();
            }
            Timeline = JsonConvert.DeserializeObject<GanjoorPresenceTimelineViewModel>(await response.Content.ReadAsStringAsync());

            return Page();
        }
    }
}
