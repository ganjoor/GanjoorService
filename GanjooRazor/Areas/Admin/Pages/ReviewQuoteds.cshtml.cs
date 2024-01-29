using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using System;
using RMuseum.Models.Ganjoor;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Text;

namespace GanjooRazor.Areas.Admin.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class ReviewQuotedsModel : PageModel
    {
        /// <summary>
        /// moderation model
        /// </summary>
        [BindProperty]
        public GanjoorQuotedPoemModerationViewModel ModerationModel { get; set; }

        /// <summary>
        /// suggestion
        /// </summary>
        public GanjoorQuotedPoemViewModel GanjoorQuotedPoem { get; set; }
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

        public Tuple<int, string>[] Couplets { get; set; }

        public Tuple<int, string>[] RelatedCouplets { get; set; }

        public GanjoorPoemCompleteViewModel Poem { get; set; }

        public GanjoorPoemCompleteViewModel RelatedPoem { get; set; }

        private Tuple<int, string>[] GetCouplets(GanjoorVerseViewModel[] verses)
        {
            int coupetIndex = -1;
            string coupletText = "";
            List<Tuple<int, string>> couplets = new List<Tuple<int, string>>();
            int verseIndex = 0;
            bool incompleteCouplet = false;
            while (verseIndex < verses.Length)
            {
                switch (verses[verseIndex].VersePosition)
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
                        couplets.Add(new Tuple<int, string>(coupetIndex, verses[verseIndex].Text));
                        break;
                    case VersePosition.Right:
                    case VersePosition.CenteredVerse1:
                        incompleteCouplet = false;
                        if (!string.IsNullOrEmpty(coupletText))
                        {
                            couplets.Add(new Tuple<int, string>(coupetIndex, coupletText));
                        }
                        coupetIndex++;
                        coupletText = verses[verseIndex].Text;
                        break;
                    case VersePosition.Left:
                    case VersePosition.CenteredVerse2:
                        incompleteCouplet = true;
                        coupletText += $" - {verses[verseIndex].Text}";
                        break;
                }
                verseIndex++;
            }


            if (incompleteCouplet && !string.IsNullOrEmpty(coupletText))
                couplets.Add(new Tuple<int, string>(coupetIndex, coupletText));

            return couplets.ToArray();
        }
        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(Request.Cookies["Token"]))
                return Redirect("/");

            FatalError = "";
            TotalCount = 0;
            Skip = string.IsNullOrEmpty(Request.Query["skip"]) ? 0 : int.Parse(Request.Query["skip"]);
            RelatedCouplets = [];
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var nextResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/quoted/suggestion/next?skip={Skip}");
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
                    GanjoorQuotedPoem = JsonConvert.DeserializeObject<GanjoorQuotedPoemViewModel>(await nextResponse.Content.ReadAsStringAsync());

                    if(GanjoorQuotedPoem != null)
                    {
                        var poemQuery = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poem/{GanjoorQuotedPoem.PoemId}");
                        if (!poemQuery.IsSuccessStatusCode)
                        {
                            FatalError = JsonConvert.DeserializeObject<string>(await poemQuery.Content.ReadAsStringAsync());
                            return Page();
                        }
                        Poem = JObject.Parse(await poemQuery.Content.ReadAsStringAsync()).ToObject<GanjoorPoemCompleteViewModel>();

                        Couplets = GetCouplets(Poem.Verses);

                        if (GanjoorQuotedPoem.RelatedPoemId != null)
                        {
                            var relPoemQuery = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poem/{GanjoorQuotedPoem.RelatedPoemId}");
                            if (!relPoemQuery.IsSuccessStatusCode)
                            {
                                FatalError = JsonConvert.DeserializeObject<string>(await relPoemQuery.Content.ReadAsStringAsync());
                                return Page();
                            }
                            RelatedPoem = JObject.Parse(await relPoemQuery.Content.ReadAsStringAsync()).ToObject<GanjoorPoemCompleteViewModel>();

                            RelatedCouplets = GetCouplets(RelatedPoem.Verses);


                           
                        }

                        ModerationModel = new GanjoorQuotedPoemModerationViewModel()
                        {
                            Id = GanjoorQuotedPoem.Id,
                            Approved = true,
                            ReviewNote = ""
                        };
                    }
                    
                }
                else
                {
                    FatalError = "لطفاً از گنجور خارج و مجددا به آن وارد شوید.";
                }
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(GanjoorQuotedPoemModerationViewModel ModerationModel)
        {
            FatalError = "";
            try
            {
                using (HttpClient secureClient = new HttpClient())
                {
                    if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                    {
                        var url = $"{APIRoot.Url}/api/ganjoor/quoted/moderate";
                        var payload = new StringContent(JsonConvert.SerializeObject(ModerationModel), Encoding.UTF8, "application/json");
                        HttpResponseMessage response = await secureClient.PutAsync(url, payload);
                        if (!response.IsSuccessStatusCode)
                        {
                            FatalError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                        }
                        else
                        {
                            Skip = string.IsNullOrEmpty(Request.Query["skip"]) ? 0 : int.Parse(Request.Query["skip"]);
                            Response.Redirect($"/Admin/ReviewQuoteds?skip={Skip}");
                        }
                    }
                    else
                    { 
                        FatalError = "لطفاً از گنجور خارج و مجددا به آن وارد شوید.";
                    }
                }
            }
            catch (Exception e)
            {
                FatalError = e.ToString();
            }
            return Page();
        }
    }
}
