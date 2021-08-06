using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;

namespace GanjooRazor.Areas.Admin.Pages
{
    public class ReviewEditsModel : PageModel
    {
        /// <summary>
        /// correction
        /// </summary>
        public GanjoorPoemCorrectionViewModel Correction { get; set; }
        /// <summary>
        /// fatal error
        /// </summary>
        public string FatalError { get; set; }

        /// <summary>
        /// skip
        /// </summary>
        public int Skip { get; set; }

        /// <summary>
        /// total count
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// page
        /// </summary>
        public GanjoorPageCompleteViewModel PageInformation { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            FatalError = "";
            TotalCount = 0;
            Skip = string.IsNullOrEmpty(Request.Query["skip"]) ? 0 : int.Parse(Request.Query["skip"]);
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var nextResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poem/correction/next?skip={Skip}");
                    nextResponse.EnsureSuccessStatusCode();

                    string paginnationMetadata = nextResponse.Headers.GetValues("paging-headers").FirstOrDefault();
                    if (!string.IsNullOrEmpty(paginnationMetadata))
                    {
                        TotalCount = JsonConvert.DeserializeObject<PaginationMetadata>(paginnationMetadata).totalCount;
                    }
                    Correction = JsonConvert.DeserializeObject<GanjoorPoemCorrectionViewModel>(await nextResponse.Content.ReadAsStringAsync());

                    var pageUrlResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/pageurl?id={Correction.PoemId}");
                    pageUrlResponse.EnsureSuccessStatusCode();
                    var pageUrl = JsonConvert.DeserializeObject<string>(await pageUrlResponse.Content.ReadAsStringAsync());

                    var pageQuery = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/page?url={pageUrl}");
                    pageQuery.EnsureSuccessStatusCode();
                    PageInformation = JObject.Parse(await pageQuery.Content.ReadAsStringAsync()).ToObject<GanjoorPageCompleteViewModel>();

                    if(Correction.Title != null)
                    {
                        Correction.OriginalTitle = PageInformation.Poem.Title;
                        if (Correction.OriginalTitle == Correction.Title)
                            Correction.Result = CorrectionReviewResult.NotChanged;
                    }

                    if(Correction.VerseOrderText != null)
                        foreach(var verse in Correction.VerseOrderText)
                        {
                            verse.OriginalText = PageInformation.Poem.Verses.Where(v => v.VOrder == verse.VORder).Single().Text;
                            if (verse.OriginalText == verse.Text)
                                verse.Result = CorrectionReviewResult.NotChanged;
                        }

                    if (Correction.Rhythm != null)
                    {
                        Correction.OriginalRhythm = PageInformation.Poem.GanjoorMetre.Rhythm;
                        if (Correction.OriginalRhythm == Correction.Rhythm)
                            Correction.RhythmResult = CorrectionReviewResult.NotChanged;
                    }
                }
                else
                {
                    FatalError = "لطفا از گنجور خارج و مجددا به آن وارد شوید.";
                }
            }
            return Page();
        }
    }
}
