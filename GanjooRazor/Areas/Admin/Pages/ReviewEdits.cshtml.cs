using System;
using System.Linq;
using System.Net.Http;
using System.Text;
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
    [IgnoreAntiforgeryToken(Order = 1001)]
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
            if (string.IsNullOrEmpty(Request.Cookies["Token"]))
                return Redirect("/");

            FatalError = "";
            TotalCount = 0;
            Skip = string.IsNullOrEmpty(Request.Query["skip"]) ? 0 : int.Parse(Request.Query["skip"]);
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var nextResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poem/correction/next?skip={Skip}");
                    if (!nextResponse.IsSuccessStatusCode)
                    {
                        FatalError = JsonConvert.DeserializeObject<string>(await nextResponse.Content.ReadAsStringAsync());
                        return Page();
                    }

                    string paginnationMetadata = nextResponse.Headers.GetValues("paging-headers").FirstOrDefault();
                    if (!string.IsNullOrEmpty(paginnationMetadata))
                    {
                        TotalCount = JsonConvert.DeserializeObject<PaginationMetadata>(paginnationMetadata).totalCount;
                    }
                    Correction = JsonConvert.DeserializeObject<GanjoorPoemCorrectionViewModel>(await nextResponse.Content.ReadAsStringAsync());
                    if(Correction != null)
                    {
                        var pageUrlResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/pageurl?id={Correction.PoemId}");
                        if (!pageUrlResponse.IsSuccessStatusCode)
                        {
                            FatalError = JsonConvert.DeserializeObject<string>(await pageUrlResponse.Content.ReadAsStringAsync());
                            return Page();
                        }
                        var pageUrl = JsonConvert.DeserializeObject<string>(await pageUrlResponse.Content.ReadAsStringAsync());

                        var pageQuery = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/page?url={pageUrl}");
                        if (!pageQuery.IsSuccessStatusCode)
                        {
                            FatalError = JsonConvert.DeserializeObject<string>(await pageQuery.Content.ReadAsStringAsync());
                            return Page();
                        }
                        PageInformation = JObject.Parse(await pageQuery.Content.ReadAsStringAsync()).ToObject<GanjoorPageCompleteViewModel>();

                        if (Correction.Title != null)
                        {
                            Correction.OriginalTitle = PageInformation.Poem.Title;
                            if (Correction.OriginalTitle == Correction.Title)
                                Correction.Result = CorrectionReviewResult.NotChanged;
                        }

                        if (Correction.VerseOrderText != null)
                            foreach (var verse in Correction.VerseOrderText)
                            {
                                var v = PageInformation.Poem.Verses.Where(v => v.VOrder == verse.VORder).Single();
                                verse.OriginalText = v.Text;
                                verse.CoupletIndex = v.CoupletIndex;
                                if (verse.OriginalText == verse.Text)
                                    verse.Result = CorrectionReviewResult.NotChanged;
                            }

                        if (Correction.Rhythm != null)
                        {
                            Correction.OriginalRhythm = PageInformation.Poem.GanjoorMetre == null ? null : PageInformation.Poem.GanjoorMetre.Rhythm;
                            if (Correction.OriginalRhythm == Correction.Rhythm)
                                Correction.RhythmResult = CorrectionReviewResult.NotChanged;
                        }
                    }
                    
                }
                else
                {
                    FatalError = "لطفا از گنجور خارج و مجددا به آن وارد شوید.";
                }
            }
            return Page();
        }

        public IActionResult OnPost()
        {
            Skip = string.IsNullOrEmpty(Request.Query["skip"]) ? 0 : int.Parse(Request.Query["skip"]);
            if (Request.Form["next"].Count == 1)
            {
                return Redirect($"/Admin/ReviewEdits/?skip={Skip + 1}");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostSendCorrectionsModerationAsync(int correctionId, 
            string titleReviewResult, string rhythmReviewResult,
            string titleReviewNote, string[] verseReviewResult,
            string[] verseReviewNotes)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var correctionResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/correction/{correctionId}");
                    if (!correctionResponse.IsSuccessStatusCode)
                    {
                        FatalError = JsonConvert.DeserializeObject<string>(await correctionResponse.Content.ReadAsStringAsync());
                        return Page();
                    }

                    Correction = JsonConvert.DeserializeObject<GanjoorPoemCorrectionViewModel>(await correctionResponse.Content.ReadAsStringAsync());

                    if (Correction.Title != null)
                    {
                        if(titleReviewResult == null)
                        {
                            return new BadRequestObjectResult("لطفا تغییر عنوان را بازبینی کنید.");
                        }
                        else
                        {
                            Correction.Result = (CorrectionReviewResult)Enum.Parse(typeof(CorrectionReviewResult), titleReviewResult);
                            Correction.ReviewNote = titleReviewNote;
                        }
                    }

                    if (Correction.Rhythm != null)
                    {
                        if(rhythmReviewResult == null)
                        {
                            return new BadRequestObjectResult("لطفا تغییر وزن را بازبینی کنید.");
                        }
                        else
                        {
                            Correction.RhythmResult = (CorrectionReviewResult)Enum.Parse(typeof(CorrectionReviewResult), rhythmReviewResult);
                            Correction.ReviewNote = titleReviewNote;
                        }
                    }

                    if(verseReviewResult.Length != Correction.VerseOrderText.Length)
                    {
                        return new BadRequestObjectResult("لطفا تکلیف بررسی تمام مصرعهای پیشنهادی را مشخص کنید.");
                    }
                    else
                    {
                        for (int i = 0; i < Correction.VerseOrderText.Length; i++)
                        {
                            Correction.VerseOrderText[i].Result = (CorrectionReviewResult)Enum.Parse(typeof(CorrectionReviewResult), verseReviewResult[i]);
                            Correction.VerseOrderText[i].ReviewNote = verseReviewNotes[i];
                        }
                    }

                    var moderationResponse = await secureClient.PostAsync($"{APIRoot.Url}/api/ganjoor/correction/moderate",
                        new StringContent(JsonConvert.SerializeObject(Correction), Encoding.UTF8, "application/json"
                        ));

                    if(!moderationResponse.IsSuccessStatusCode)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await moderationResponse.Content.ReadAsStringAsync()));
                    }

                    return new OkObjectResult(true);
                }
                else
                {
                    return new BadRequestObjectResult("لطفا از گنجور خارج و مجددا به آن وارد شوید.");
                }
            }
        }
    }
}
