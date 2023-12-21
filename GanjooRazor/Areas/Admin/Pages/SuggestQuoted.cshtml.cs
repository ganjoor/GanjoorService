using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor.ViewModels;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using RMuseum.Models.Ganjoor;

namespace GanjooRazor.Areas.Admin.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class SuggestQuotedModel : PageModel
    {
        public string LastMessage { get; set; }

        /// <summary>
        /// HttpClient instance
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="httpClient"></param>
        public SuggestQuotedModel(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public GanjoorPoemCompleteViewModel Poem { get; set; }

        public Tuple<int, string>[] Couplets { get; set; }

        [BindProperty]
        public GanjoorQuotedPoem GanjoorQuotedPoem { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(Request.Cookies["Token"]))
                return Redirect("/");


            if (string.IsNullOrEmpty(Request.Query["p"]))
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "شناسهٔ شعر مشخص نیست.");
            }

            int poemId = int.Parse(Request.Query["p"]);

            

            var pageUrlResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/pageurl?id={poemId}");
            if (!pageUrlResponse.IsSuccessStatusCode)
            {
                LastMessage = JsonConvert.DeserializeObject<string>(await pageUrlResponse.Content.ReadAsStringAsync());
                return Page();
            }

            var pageUrl = JsonConvert.DeserializeObject<string>(await pageUrlResponse.Content.ReadAsStringAsync());

            var pageQuery = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/page?url={pageUrl}");
            if (!pageQuery.IsSuccessStatusCode)
            {
                LastMessage = JsonConvert.DeserializeObject<string>(await pageQuery.Content.ReadAsStringAsync());
                return Page();
            }
            var pageInformation = JObject.Parse(await pageQuery.Content.ReadAsStringAsync()).ToObject<GanjoorPageCompleteViewModel>();

            Poem = pageInformation.Poem;

            int coupetIndex = -1;
            string coupletText = "";
            List<Tuple<int, string>> couplets = new List<Tuple<int, string>>();
            int verseIndex = 0;
            bool incompleteCouplet = false;
            while (verseIndex < pageInformation.Poem.Verses.Length)
            {

                switch (pageInformation.Poem.Verses[verseIndex].VersePosition)
                {
                    case VersePosition.Comment:
                        incompleteCouplet = false;
                        if (!string.IsNullOrEmpty(coupletText))
                        {
                            couplets.Add(new Tuple<int, string>(coupetIndex, coupletText));
                            coupletText = "";
                        }
                        break;
                    case VersePosition.Paragraph:
                    case VersePosition.Single:
                        incompleteCouplet = false;
                        if (!string.IsNullOrEmpty(coupletText))
                        {
                            couplets.Add(new Tuple<int, string>(coupetIndex, coupletText));
                            coupletText = "";
                        }
                        coupetIndex++;
                        couplets.Add(new Tuple<int, string>(coupetIndex, pageInformation.Poem.Verses[verseIndex].Text));
                        break;
                    case VersePosition.Right:
                    case VersePosition.CenteredVerse1:
                        incompleteCouplet = false;
                        if (!string.IsNullOrEmpty(coupletText))
                        {
                            couplets.Add(new Tuple<int, string>(coupetIndex, coupletText));
                        }
                        coupetIndex++;
                        coupletText = pageInformation.Poem.Verses[verseIndex].Text;
                        break;
                    case VersePosition.Left:
                    case VersePosition.CenteredVerse2:
                        incompleteCouplet = true;
                        coupletText += $" - {pageInformation.Poem.Verses[verseIndex].Text}";
                        break;
                }
                verseIndex++;
            }


            if (incompleteCouplet && !string.IsNullOrEmpty(coupletText))
                couplets.Add(new Tuple<int, string>(coupetIndex, coupletText));

            Couplets = couplets.ToArray();

            GanjoorQuotedPoem = new GanjoorQuotedPoem()
            {
                PoemId = poemId,
                PoetId = Poem.Category.Poet.Id,
                RelatedPoetId = null,
                RelatedPoemId = null,
                IsPriorToRelated = false,
                ChosenForMainList = true,
                CachedRelatedPoemPoetDeathYearInLHijri = 0,
                CachedRelatedPoemPoetName = null,
                CachedRelatedPoemPoetUrl = null,
                CachedRelatedPoemPoetImage = null,
                CachedRelatedPoemFullTitle = null,
                CachedRelatedPoemFullUrl = null,
                SortOrder = 1000,
                Note = "",
                Published = false,
                ClaimedByBothPoets = false,
                IndirectQuotation = false,
                SamePoemsQuotedCount = 0,
                RelatedCoupletVerse1 = null,
                RelatedCoupletVerse1ShouldBeEmphasized = false,
                RelatedCoupletVerse2 = null,
                RelatedCoupletVerse2ShouldBeEmphasized = false,
                RelatedCoupletIndex = null,
                CoupletVerse1 = Poem.Verses[0].Text,
                CoupletVerse1ShouldBeEmphasized = false,
                CoupletVerse2 = Poem.Verses[1].Text,
                CoupletVerse2ShouldBeEmphasized = false,
                CoupletIndex = 0,

            };

            return Page();
        }
    }
}
