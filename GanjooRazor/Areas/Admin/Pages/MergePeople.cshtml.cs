using GanjooRazor.Pages;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GanjooRazor.Areas.Admin.Pages
{
    /// <summary>
    /// merge duplicate people and edit the aliases (other names) of a person
    /// </summary>
    public class MergePeopleModel : GanjoorPageModelBase
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="httpClient"></param>
        public MergePeopleModel(HttpClient httpClient) : base(httpClient)
        {
        }

        /// <summary>
        /// all people
        /// </summary>
        public GanjoorRelatedPerson[] People { get; set; }

        /// <summary>
        /// error
        /// </summary>
        public string LastMessage { get; set; }

        /// <summary>
        /// get
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(Request.Cookies["Token"]))
                return Redirect("/");

            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/people");
            if (!response.IsSuccessStatusCode)
            {
                LastMessage = await ReadErrorMessageAsync(response);
                People = [];
                return Page();
            }
            People = JsonConvert.DeserializeObject<GanjoorRelatedPerson[]>(await response.Content.ReadAsStringAsync());
            return Page();
        }

        /// <summary>
        /// merge source into target
        /// </summary>
        /// <param name="sourceId"></param>
        /// <param name="targetId"></param>
        /// <returns></returns>
        public Task<IActionResult> OnPostMergeAsync(int sourceId, int targetId)
        {
            return WithSecureClientAsync(async secureClient =>
            {
                var content = new StringContent(JsonConvert.SerializeObject(new PersonMergeViewModel() { SourceId = sourceId, TargetId = targetId }), Encoding.UTF8, "application/json");
                var response = await secureClient.PostAsync($"{APIRoot.Url}/api/people/merge", content);
                if (!response.IsSuccessStatusCode)
                {
                    return new BadRequestObjectResult(await ReadErrorMessageAsync(response));
                }
                return new OkObjectResult(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
            }, new OkObjectResult(false));
        }

        /// <summary>
        /// save aliases
        /// </summary>
        /// <param name="id"></param>
        /// <param name="aliases"></param>
        /// <returns></returns>
        public Task<IActionResult> OnPostAliasesAsync(int id, string aliases)
        {
            return WithSecureClientAsync(async secureClient =>
            {
                var content = new StringContent(JsonConvert.SerializeObject(new PersonAliasesViewModel() { Aliases = aliases }), Encoding.UTF8, "application/json");
                var response = await secureClient.PutAsync($"{APIRoot.Url}/api/people/{id}/aliases", content);
                if (!response.IsSuccessStatusCode)
                {
                    return new BadRequestObjectResult(await ReadErrorMessageAsync(response));
                }
                var person = JsonConvert.DeserializeObject<GanjoorRelatedPerson>(await response.Content.ReadAsStringAsync());
                return new OkObjectResult(person.Aliases ?? "");
            }, new OkObjectResult(false));
        }
    }
}
