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
using GanjooRazor.Utils;
using System.Text;
using System.Linq;
using System.Net;

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

        public GanjoorPoemCompleteViewModel RelatedPoem { get; set; }

        public Tuple<int, string>[] Couplets { get; set; }

        public Tuple<int, string>[] RelatedCouplets { get; set; }

        [BindProperty]
        public GanjoorQuotedPoem GanjoorQuotedPoem { get; set; }

        public GanjoorQuotedPoem[] AllPoemQuoteds { get; set; }
        public Guid? ReverseId { get; set; }

        public bool DisplayAll { get; set; }

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

        private async Task<string> Prepare(int poemId, string id)
        {
            LastMessage = "";
            var poemQuery = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poem/{poemId}");
            if (!poemQuery.IsSuccessStatusCode)
            {
                LastMessage = JsonConvert.DeserializeObject<string>(await poemQuery.Content.ReadAsStringAsync());
                return LastMessage;
            }
            Poem = JObject.Parse(await poemQuery.Content.ReadAsStringAsync()).ToObject<GanjoorPoemCompleteViewModel>();

            Couplets = GetCouplets(Poem.Verses);
            RelatedCouplets = [];

            if (!string.IsNullOrEmpty(id))
            {

                var quoteQuery = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/quoted/{id}");
                if (!quoteQuery.IsSuccessStatusCode)
                {
                    LastMessage = JsonConvert.DeserializeObject<string>(await quoteQuery.Content.ReadAsStringAsync());
                    return LastMessage;
                }
                GanjoorQuotedPoem = JObject.Parse(await quoteQuery.Content.ReadAsStringAsync()).ToObject<GanjoorQuotedPoem>();
                if (GanjoorQuotedPoem.RelatedPoemId != null)
                {
                    var relPoemQuery = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poem/{GanjoorQuotedPoem.RelatedPoemId}");
                    if (!relPoemQuery.IsSuccessStatusCode)
                    {
                        LastMessage = JsonConvert.DeserializeObject<string>(await relPoemQuery.Content.ReadAsStringAsync());
                        return LastMessage;
                    }
                    RelatedPoem = JObject.Parse(await relPoemQuery.Content.ReadAsStringAsync()).ToObject<GanjoorPoemCompleteViewModel>();

                    RelatedCouplets = GetCouplets(RelatedPoem.Verses);


                    var revQuery = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poem/{GanjoorQuotedPoem.RelatedPoemId}/quoteds/{GanjoorQuotedPoem.PoemId}");
                    if (!revQuery.IsSuccessStatusCode)
                    {
                        LastMessage = JsonConvert.DeserializeObject<string>(await revQuery.Content.ReadAsStringAsync());
                        return LastMessage;
                    }

                    var revs = JsonConvert.DeserializeObject<GanjoorQuotedPoem[]>(await revQuery.Content.ReadAsStringAsync());
                    var rev = revs.Where(r => r.CoupletIndex == GanjoorQuotedPoem.RelatedCoupletIndex && r.RelatedCoupletIndex == GanjoorQuotedPoem.CoupletIndex).SingleOrDefault();
                    if (rev != null)
                    {
                        ReverseId = rev.Id;
                    }
                }

            }
            else
            {
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
            }

            var allQuery = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poem/{poemId}/quoteds");
            if (!allQuery.IsSuccessStatusCode)
            {
                LastMessage = JsonConvert.DeserializeObject<string>(await allQuery.Content.ReadAsStringAsync());
                return LastMessage;
            }
            AllPoemQuoteds = JsonConvert.DeserializeObject<GanjoorQuotedPoem[]>(await allQuery.Content.ReadAsStringAsync());
            return LastMessage;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(Request.Cookies["Token"]))
                return Redirect("/");

            DisplayAll = false;
            if (!string.IsNullOrEmpty(Request.Query["all"]))
            {
                DisplayAll = true;
                string url = $"{APIRoot.Url}/api/ganjoor/quoted?published=false";
                if (!string.IsNullOrEmpty(Request.Query["p"]))
                {
                    url += $"&poetId={Request.Query["p"]}";
                }
                if (!string.IsNullOrEmpty(Request.Query["r"]))
                {
                    url += $"&relatedPoetId={Request.Query["r"]}";
                }
                var allQuery = await _httpClient.GetAsync(url);
                if (!allQuery.IsSuccessStatusCode)
                {
                    LastMessage = JsonConvert.DeserializeObject<string>(await allQuery.Content.ReadAsStringAsync());
                }
                AllPoemQuoteds = JsonConvert.DeserializeObject<GanjoorQuotedPoem[]>(await allQuery.Content.ReadAsStringAsync());

                return Page();
            }

            string poemIdString = Request.Query["p"];
            if (string.IsNullOrEmpty(poemIdString))
            {
                if (!string.IsNullOrEmpty(Request.Query["id"]))
                {
                    var quoteQuery = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/quoted/{Request.Query["id"]}");
                    if (!quoteQuery.IsSuccessStatusCode)
                    {
                        LastMessage = JsonConvert.DeserializeObject<string>(await quoteQuery.Content.ReadAsStringAsync());
                        return Page();
                    }
                    GanjoorQuotedPoem = JObject.Parse(await quoteQuery.Content.ReadAsStringAsync()).ToObject<GanjoorQuotedPoem>();
                    poemIdString = GanjoorQuotedPoem.PoemId.ToString();
                }
                else
                {
                    LastMessage = "شناسهٔ شعر مشخص نیست.";
                    return Page();
                }

            }

            await Prepare(int.Parse(poemIdString), Request.Query["id"]);



            return Page();
        }

        public async Task<IActionResult> OnPostAsync(GanjoorQuotedPoem GanjoorQuotedPoem)
        {
            try
            {

                await Prepare(GanjoorQuotedPoem.PoemId, GanjoorQuotedPoem.Id == Guid.Empty ? null : GanjoorQuotedPoem.Id.ToString());
                GanjoorQuotedPoem.CoupletVerse1 = Poem.Verses.Where(v => v.CoupletIndex == GanjoorQuotedPoem.CoupletIndex).ToArray()[0].Text;
                GanjoorQuotedPoem.CoupletVerse2 = Poem.Verses.Where(v => v.CoupletIndex == GanjoorQuotedPoem.CoupletIndex).ToArray()[1].Text;
                if (GanjoorQuotedPoem.RelatedPoemId != null && RelatedPoem == null)
                {
                    var relPoemQuery = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poem/{GanjoorQuotedPoem.RelatedPoemId}");
                    if (!relPoemQuery.IsSuccessStatusCode)
                    {
                        LastMessage = JsonConvert.DeserializeObject<string>(await relPoemQuery.Content.ReadAsStringAsync());
                        return Page();
                    }
                    RelatedPoem = JObject.Parse(await relPoemQuery.Content.ReadAsStringAsync()).ToObject<GanjoorPoemCompleteViewModel>();

                    RelatedCouplets = GetCouplets(RelatedPoem.Verses);
                }

                if (GanjoorQuotedPoem.RelatedCoupletIndex != null)
                {
                    GanjoorQuotedPoem.RelatedCoupletVerse1 = RelatedPoem.Verses.Where(v => v.CoupletIndex == GanjoorQuotedPoem.RelatedCoupletIndex).ToArray()[0].Text;
                    GanjoorQuotedPoem.RelatedCoupletVerse2 = RelatedPoem.Verses.Where(v => v.CoupletIndex == GanjoorQuotedPoem.RelatedCoupletIndex).ToArray()[1].Text;
                }
                using (HttpClient secureClient = new HttpClient())
                {
                    if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                    {
                        var url = $"{APIRoot.Url}/api/ganjoor/quoted";
                        var payload = new StringContent(JsonConvert.SerializeObject(GanjoorQuotedPoem), Encoding.UTF8, "application/json");
                        bool newRecord = GanjoorQuotedPoem.Id == Guid.Empty;
                        HttpResponseMessage response =
                            newRecord ?
                            await secureClient.PostAsync(url, payload) :
                            await secureClient.PutAsync(url, payload);
                        if (!response.IsSuccessStatusCode)
                        {
                            LastMessage = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                        }
                        else
                        {
                            if (newRecord)
                            {
                                GanjoorQuotedPoem = JObject.Parse(await response.Content.ReadAsStringAsync()).ToObject<GanjoorQuotedPoem>();
                            }

                            LastMessage = $"انجام شد. <br /><a href=\"/Admin/SuggestQuoted/?p={GanjoorQuotedPoem.PoemId}&id={GanjoorQuotedPoem.Id}\">برگشت</a>";

                        }
                    }
                    else
                    {
                        LastMessage = "لطفاً از گنجور خارج و مجددا به آن وارد شوید.";
                    }

                }
            }
            catch (Exception e)
            {
                LastMessage = e.ToString();
            }
            return Page();
        }

        public async Task<IActionResult> OnDeleteAsync(string id)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var response = await secureClient.DeleteAsync($"{APIRoot.Url}/api/ganjoor/quoted?id={id}");

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()));
                    }

                }
                else
                {
                    return new BadRequestObjectResult("لطفاً از گنجور خارج و مجددا به آن وارد شوید.");
                }
            }
            return new JsonResult(true);
        }

        public async Task<IActionResult> OnPostGenerateReverseAsync(string id)
        {
            var quoteQuery = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/quoted/{id}");
            if (!quoteQuery.IsSuccessStatusCode)
            {
                return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await quoteQuery.Content.ReadAsStringAsync()));
            }
            var quote = JObject.Parse(await quoteQuery.Content.ReadAsStringAsync()).ToObject<GanjoorQuotedPoem>();

            var poemQuery = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poem/{quote.PoemId}");
            if (!poemQuery.IsSuccessStatusCode)
            {
                LastMessage = JsonConvert.DeserializeObject<string>(await poemQuery.Content.ReadAsStringAsync());
                return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await quoteQuery.Content.ReadAsStringAsync()));
            }
            Poem = JObject.Parse(await poemQuery.Content.ReadAsStringAsync()).ToObject<GanjoorPoemCompleteViewModel>();

            GanjoorQuotedPoem reverseRelation = new GanjoorQuotedPoem()
            {
                PoemId = (int)quote.RelatedPoemId,
                PoetId = (int)quote.RelatedPoetId,
                RelatedPoetId = quote.PoetId,
                RelatedPoemId = quote.PoemId,
                IsPriorToRelated = !quote.IsPriorToRelated,
                ChosenForMainList = true,
                CachedRelatedPoemPoetDeathYearInLHijri = Poem.Category.Poet.DeathYearInLHijri,
                CachedRelatedPoemPoetName = Poem.Category.Poet.Nickname,
                CachedRelatedPoemPoetUrl = Poem.Category.Poet.FullUrl,
                CachedRelatedPoemPoetImage = $"/api/ganjoor/poet/image{Poem.Category.Poet.FullUrl}.gif",
                CachedRelatedPoemFullTitle = Poem.FullTitle,
                CachedRelatedPoemFullUrl = Poem.FullUrl,
                SortOrder = 1000,
                Note = quote.Note,
                Published = false,
                RelatedCoupletVerse1 = quote.CoupletVerse1,
                RelatedCoupletVerse1ShouldBeEmphasized = quote.CoupletVerse1ShouldBeEmphasized,
                RelatedCoupletVerse2 = quote.CoupletVerse2,
                RelatedCoupletVerse2ShouldBeEmphasized = quote.CoupletVerse2ShouldBeEmphasized,
                RelatedCoupletIndex = quote.CoupletIndex,
                CoupletVerse1 = quote.RelatedCoupletVerse1,
                CoupletVerse1ShouldBeEmphasized = quote.RelatedCoupletVerse1ShouldBeEmphasized,
                CoupletVerse2 = quote.RelatedCoupletVerse2,
                CoupletVerse2ShouldBeEmphasized = quote.RelatedCoupletVerse2ShouldBeEmphasized,
                CoupletIndex = quote.RelatedCoupletIndex,
                ClaimedByBothPoets = quote.ClaimedByBothPoets,
                IndirectQuotation = quote.IndirectQuotation,
                SamePoemsQuotedCount = 1,

            };

            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var url = $"{APIRoot.Url}/api/ganjoor/quoted";
                    var payload = new StringContent(JsonConvert.SerializeObject(reverseRelation), Encoding.UTF8, "application/json");

                    HttpResponseMessage response =

                        await secureClient.PostAsync(url, payload)
                        ;
                    if (!response.IsSuccessStatusCode)
                    {
                        return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>(await quoteQuery.Content.ReadAsStringAsync()));
                    }
                    else
                    {
                        reverseRelation = JObject.Parse(await response.Content.ReadAsStringAsync()).ToObject<GanjoorQuotedPoem>();
                        return new JsonResult($"/Admin/SuggestQuoted/?p={reverseRelation.PoemId}&id={reverseRelation.Id}");
                    }
                }
                else
                {
                    return new BadRequestObjectResult(JsonConvert.DeserializeObject<string>("لطفاً از گنجور خارج و مجددا به آن وارد شوید."));

                }

            }


        }
    }
}
