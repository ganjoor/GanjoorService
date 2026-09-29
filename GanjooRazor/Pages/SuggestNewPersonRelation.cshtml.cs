using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor;

namespace GanjooRazor.Pages
{
    /// <summary>
    /// contributor-facing form for suggesting a brand new kinship edge between the subject person
    /// and another already-approved person - the "Add" counterpart of SuggestPersonRelationEdit.cshtml
    /// (which only handles Modify/Remove of an already-existing edge). Presents a direction-aware
    /// "RelationKind" choice (e.g. "فرزند" vs "پدر یا مادر") from the subject's point of view, and
    /// translates it into the correct Person1Id/Person2Id/RelationType triple before submitting -
    /// GanjoorPersonRelation itself has no notion of "from the subject's point of view", only
    /// Person1/Person2, so that translation has to happen somewhere, and doing it here keeps the
    /// underlying suggestion/moderation code simple and symmetric.
    /// </summary>
    public class SuggestNewPersonRelationModel : LoginPartialEnabledPageModel
    {
        public SuggestNewPersonRelationModel(HttpClient httpClient, IConfiguration configuration) : base(httpClient, configuration)
        {
        }

        public string LastError { get; set; }

        public string LastResult { get; set; }

        public GanjoorRelatedPerson Person { get; set; }

        public List<GanjoorRelatedPerson> OtherPeople { get; set; }

        [BindProperty]
        public int OtherPersonId { get; set; }

        [BindProperty]
        public string RelationKind { get; set; }

        [BindProperty]
        public int? DegreeHint { get; set; }

        [BindProperty]
        public string Note { get; set; }

        [BindProperty]
        public string SuggestionNote { get; set; }

        private async Task<bool> PreparePersonAsync(int id)
        {
            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/people/{id}");
            if (!response.IsSuccessStatusCode)
            {
                LastError = await ReadErrorMessageAsync(response);
                return false;
            }

            Person = JsonConvert.DeserializeObject<GanjoorRelatedPerson>(await response.Content.ReadAsStringAsync());
            if (Person == null)
            {
                LastError = "شخصیتی با این کد پیدا نشد.";
                return false;
            }

            var peopleResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/people");
            if (!peopleResponse.IsSuccessStatusCode)
            {
                LastError = await ReadErrorMessageAsync(peopleResponse);
                return false;
            }
            var allPeople = JsonConvert.DeserializeObject<List<GanjoorRelatedPerson>>(await peopleResponse.Content.ReadAsStringAsync());
            OtherPeople = allPeople.Where(p => p.Id != id).ToList();

            return true;
        }

        public async Task<IActionResult> OnGetAsync(int personId)
        {
            InitializeCommonPageState();

            if (!LoggedIn)
            {
                return Redirect($"/login?redirect={Uri.EscapeDataString(Request.Path)}");
            }

            await PreparePersonAsync(personId);

            return Page();
        }

        public async Task<IActionResult> OnPostSuggestAsync(int personId)
        {
            InitializeCommonPageState();

            if (!LoggedIn)
            {
                return Redirect($"/login?redirect={Uri.EscapeDataString(Request.Path)}");
            }

            LastResult = "";

            if (!await PreparePersonAsync(personId))
            {
                return Page();
            }

            if (OtherPersonId == 0 || OtherPersonId == personId)
            {
                LastResult = "لطفاً خویشاوند مورد نظر را انتخاب کنید.";
                return Page();
            }

            // translate the direction-aware choice (from Person's own point of view) into the
            // symmetric Person1Id/Person2Id/RelationType triple GanjoorPersonRelation actually stores
            var suggestion = new GanjoorPersonRelationEditSuggestion()
            {
                Action = PersonRelationSuggestionAction.Add,
                SuggestedDegreeHint = DegreeHint,
                SuggestedNote = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim(),
                SuggestionNote = string.IsNullOrWhiteSpace(SuggestionNote) ? null : SuggestionNote.Trim(),
            };

            switch (RelationKind)
            {
                case "Child": // OtherPerson is a child of Person
                    suggestion.Person1Id = personId;
                    suggestion.Person2Id = OtherPersonId;
                    suggestion.SuggestedRelationType = PersonRelationType.Parent;
                    break;
                case "Parent": // OtherPerson is a parent of Person
                    suggestion.Person1Id = OtherPersonId;
                    suggestion.Person2Id = personId;
                    suggestion.SuggestedRelationType = PersonRelationType.Parent;
                    break;
                case "Sibling":
                    suggestion.Person1Id = personId;
                    suggestion.Person2Id = OtherPersonId;
                    suggestion.SuggestedRelationType = PersonRelationType.Sibling;
                    break;
                case "Spouse":
                    suggestion.Person1Id = personId;
                    suggestion.Person2Id = OtherPersonId;
                    suggestion.SuggestedRelationType = PersonRelationType.Spouse;
                    break;
                case "DistantAncestor": // OtherPerson is a distant ancestor of Person
                    suggestion.Person1Id = OtherPersonId;
                    suggestion.Person2Id = personId;
                    suggestion.SuggestedRelationType = PersonRelationType.Ancestor;
                    break;
                case "DistantDescendant": // OtherPerson is a distant descendant of Person
                    suggestion.Person1Id = personId;
                    suggestion.Person2Id = OtherPersonId;
                    suggestion.SuggestedRelationType = PersonRelationType.Ancestor;
                    break;
                default:
                    LastResult = "نوع نسبت نامعتبر است.";
                    return Page();
            }

            using (HttpClient secureClient = new HttpClient(new GanjoorReloginHandler(Request, Response)))
            {
                await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response);

                HttpResponseMessage response = await secureClient.PostAsync(
                    $"{APIRoot.Url}/api/people/relationeditsuggestion",
                    new StringContent(JsonConvert.SerializeObject(suggestion), Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                {
                    LastResult = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                    return Page();
                }

                LastResult = $"پیشنهاد شما ثبت شد و پس از بررسی توسط مدیران اعمال خواهد شد. <a role=\"button\" href=\"/person/{personId}\" class=\"actionlink\">بازگشت به صفحهٔ شخصیت</a>";

                return Page();
            }
        }
    }
}
