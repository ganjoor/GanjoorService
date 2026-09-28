using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor;

namespace GanjooRazor.Pages
{
    /// <summary>
    /// entry point for browsing family trees - lists every person that captions a tree
    /// (GanjoorRelatedPerson.FamilyTreeCaption not empty), grouped under that caption. A person
    /// with no caption of their own still shows up via their relatives on that person's own page
    /// (see Person.cshtml) - this index is only for jumping straight to a named tree's root(s).
    /// </summary>
    public class PeopleModel : LoginPartialEnabledPageModel
    {
        public PeopleModel(HttpClient httpClient, IConfiguration configuration) : base(httpClient, configuration)
        {
        }

        public string LastError { get; set; }

        public List<GanjoorRelatedPerson> FamilyTreeRoots { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            InitializeCommonPageState();

            ViewData["Title"] = "گنجور » شجره‌نامه‌ها";

            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/people/familytrees");
            if (!response.IsSuccessStatusCode)
            {
                LastError = await ReadErrorMessageAsync(response);
                return Page();
            }
            FamilyTreeRoots = JsonConvert.DeserializeObject<List<GanjoorRelatedPerson>>(await response.Content.ReadAsStringAsync());

            return Page();
        }
    }
}
