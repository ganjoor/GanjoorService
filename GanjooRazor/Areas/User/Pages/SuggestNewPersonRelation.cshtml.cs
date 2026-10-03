using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using GanjooRazor.Pages;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor;

namespace GanjooRazor.Areas.User.Pages
{
    /// <summary>
    /// contributor-facing form for suggesting a brand new edge between the subject person and
    /// another already-approved person - the "Add" counterpart of SuggestPersonRelationEdit.cshtml
    /// (which only handles Modify/Remove of an already-existing edge). Covers both kinds of edge:
    /// a family/kinship tie (GanjoorPersonRelation, via RelationKind) and a non-family affiliation
    /// tie (GanjoorPersonAffiliation, via AffiliationKind) - RelationKindGroup picks which. Both
    /// present a direction-aware choice from the subject's point of view (e.g. "فرزند" vs "پدر یا
    /// مادر", or "او وزیر این شخصیت بود" vs "این شخصیت وزیر او بود") and translate it into the
    /// symmetric Person1Id/Person2Id/type triple the underlying entity actually stores - neither
    /// GanjoorPersonRelation nor GanjoorPersonAffiliation has any notion of "from the subject's own
    /// point of view", only Person1/Person2, so that translation has to happen somewhere, and doing
    /// it here keeps the underlying suggestion/moderation code simple and symmetric.
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

        /// <summary>
        /// "family" (default) or "affiliation" - which of RelationKind/AffiliationKind below applies
        /// </summary>
        [BindProperty]
        public string RelationKindGroup { get; set; }

        [BindProperty]
        public string RelationKind { get; set; }

        /// <summary>
        /// combined type+direction key for a non-family tie, e.g. "Minister_Subject" meaning
        /// "this person served as minister to the other person" - see the switch in
        /// OnPostSuggestAsync for the full list and their direction handling
        /// </summary>
        [BindProperty]
        public string AffiliationKind { get; set; }

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
                LastError = "نامبرده‌ای با این کد پیدا نشد.";
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
                return Redirect($"/login?redirect={Uri.EscapeDataString(Request.Path + Request.QueryString)}");
            }

            await PreparePersonAsync(personId);

            return Page();
        }

        public async Task<IActionResult> OnPostSuggestAsync(int personId)
        {
            InitializeCommonPageState();

            if (!LoggedIn)
            {
                return Redirect($"/login?redirect={Uri.EscapeDataString(Request.Path + Request.QueryString)}");
            }

            LastResult = "";

            if (!await PreparePersonAsync(personId))
            {
                return Page();
            }

            if (OtherPersonId == 0 || OtherPersonId == personId)
            {
                LastResult = "لطفاً خویشاوند یا نامبردۀ مورد نظر را انتخاب کنید.";
                return Page();
            }

            var suggestion = new GanjoorPersonRelationEditSuggestion()
            {
                Action = PersonRelationSuggestionAction.Add,
                SuggestedDegreeHint = DegreeHint,
                SuggestedNote = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim(),
                SuggestionNote = string.IsNullOrWhiteSpace(SuggestionNote) ? null : SuggestionNote.Trim(),
            };

            if (RelationKindGroup == "affiliation")
            {
                suggestion.Kind = PersonRelationSuggestionKind.Affiliation;

                // OtherIsPerson1/SubjectIsPerson1 pairs translate the direction-aware choice into the
                // symmetric Person1Id/Person2Id pair, following the direction each PersonAffiliationType
                // value's own doc comment defines (Minister/Advisor/Courtier/Servant: Person1 is the
                // subordinate one; Patron/Successor: Person1 is the patron/later one; Panegyrized/
                // Satirized: Person1 is the poet; Ally/Rival/Companion/Other: symmetric, direction
                // doesn't matter)
                switch (AffiliationKind)
                {
                    case "Minister_Other": // the other person served as minister to this one
                        suggestion.Person1Id = OtherPersonId;
                        suggestion.Person2Id = personId;
                        suggestion.SuggestedAffiliationType = PersonAffiliationType.Minister;
                        break;
                    case "Minister_Subject": // this person served as minister to the other one
                        suggestion.Person1Id = personId;
                        suggestion.Person2Id = OtherPersonId;
                        suggestion.SuggestedAffiliationType = PersonAffiliationType.Minister;
                        break;
                    case "Advisor_Other":
                        suggestion.Person1Id = OtherPersonId;
                        suggestion.Person2Id = personId;
                        suggestion.SuggestedAffiliationType = PersonAffiliationType.Advisor;
                        break;
                    case "Advisor_Subject":
                        suggestion.Person1Id = personId;
                        suggestion.Person2Id = OtherPersonId;
                        suggestion.SuggestedAffiliationType = PersonAffiliationType.Advisor;
                        break;
                    case "Courtier_Other":
                        suggestion.Person1Id = OtherPersonId;
                        suggestion.Person2Id = personId;
                        suggestion.SuggestedAffiliationType = PersonAffiliationType.Courtier;
                        break;
                    case "Courtier_Subject":
                        suggestion.Person1Id = personId;
                        suggestion.Person2Id = OtherPersonId;
                        suggestion.SuggestedAffiliationType = PersonAffiliationType.Courtier;
                        break;
                    case "Servant_Other":
                        suggestion.Person1Id = OtherPersonId;
                        suggestion.Person2Id = personId;
                        suggestion.SuggestedAffiliationType = PersonAffiliationType.Servant;
                        break;
                    case "Servant_Subject":
                        suggestion.Person1Id = personId;
                        suggestion.Person2Id = OtherPersonId;
                        suggestion.SuggestedAffiliationType = PersonAffiliationType.Servant;
                        break;
                    case "Patron_Other": // the other person was patron of this one
                        suggestion.Person1Id = OtherPersonId;
                        suggestion.Person2Id = personId;
                        suggestion.SuggestedAffiliationType = PersonAffiliationType.Patron;
                        break;
                    case "Patron_Subject": // this person was patron of the other one
                        suggestion.Person1Id = personId;
                        suggestion.Person2Id = OtherPersonId;
                        suggestion.SuggestedAffiliationType = PersonAffiliationType.Patron;
                        break;
                    case "Successor_Other": // the other person succeeded this one
                        suggestion.Person1Id = OtherPersonId;
                        suggestion.Person2Id = personId;
                        suggestion.SuggestedAffiliationType = PersonAffiliationType.Successor;
                        break;
                    case "Successor_Subject": // this person succeeded the other one
                        suggestion.Person1Id = personId;
                        suggestion.Person2Id = OtherPersonId;
                        suggestion.SuggestedAffiliationType = PersonAffiliationType.Successor;
                        break;
                    case "Panegyrized_Other": // the other person (a poet) wrote praise poetry about this one
                        suggestion.Person1Id = OtherPersonId;
                        suggestion.Person2Id = personId;
                        suggestion.SuggestedAffiliationType = PersonAffiliationType.Panegyrized;
                        break;
                    case "Panegyrized_Subject": // this person (a poet) wrote praise poetry about the other one
                        suggestion.Person1Id = personId;
                        suggestion.Person2Id = OtherPersonId;
                        suggestion.SuggestedAffiliationType = PersonAffiliationType.Panegyrized;
                        break;
                    case "Satirized_Other": // the other person (a poet) wrote satirical poetry about this one
                        suggestion.Person1Id = OtherPersonId;
                        suggestion.Person2Id = personId;
                        suggestion.SuggestedAffiliationType = PersonAffiliationType.Satirized;
                        break;
                    case "Satirized_Subject": // this person (a poet) wrote satirical poetry about the other one
                        suggestion.Person1Id = personId;
                        suggestion.Person2Id = OtherPersonId;
                        suggestion.SuggestedAffiliationType = PersonAffiliationType.Satirized;
                        break;
                    case "Ally":
                        suggestion.Person1Id = personId;
                        suggestion.Person2Id = OtherPersonId;
                        suggestion.SuggestedAffiliationType = PersonAffiliationType.Ally;
                        break;
                    case "Rival":
                        suggestion.Person1Id = personId;
                        suggestion.Person2Id = OtherPersonId;
                        suggestion.SuggestedAffiliationType = PersonAffiliationType.Rival;
                        break;
                    case "Companion":
                        suggestion.Person1Id = personId;
                        suggestion.Person2Id = OtherPersonId;
                        suggestion.SuggestedAffiliationType = PersonAffiliationType.Companion;
                        break;
                    case "Other":
                        suggestion.Person1Id = personId;
                        suggestion.Person2Id = OtherPersonId;
                        suggestion.SuggestedAffiliationType = PersonAffiliationType.Other;
                        break;
                    default:
                        LastResult = "نوع وابستگی نامعتبر است.";
                        return Page();
                }
            }
            else
            {
                suggestion.Kind = PersonRelationSuggestionKind.Family;

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

                LastResult = $"پیشنهاد شما ثبت شد و پس از بررسی توسط مدیران اعمال خواهد شد. <a role=\"button\" href=\"javascript:void(0)\" onclick=\"PersonWindow.open({personId})\" class=\"actionlink\">مشاهدهٔ اطلاعات این نامبرده</a>";

                return Page();
            }
        }
    }
}
