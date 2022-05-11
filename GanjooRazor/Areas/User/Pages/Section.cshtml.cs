using System.Collections.Generic;
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

namespace GanjooRazor.Areas.User.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class SectionModel : PageModel
    {
        /// <summary>
        /// fatal error
        /// </summary>
        public string FatalError { get; set; }

        /// <summary>
        /// page
        /// </summary>
        public GanjoorPageCompleteViewModel PageInformation { get; set; }

        /// <summary>
        /// section
        /// </summary>
        public GanjoorPoemSection PoemSection { get; set; }

        /// <summary>
        /// rhythms alphabetically
        /// </summary>
        public GanjoorMetre[] RhythmsAlphabetically { get; set; }

        /// <summary>
        /// rhythms by frequency
        /// </summary>
        public GanjoorMetre[] RhythmsByVerseCount { get; set; }

        /// <summary>
        /// my last edit
        /// </summary>
        public GanjoorPoemCorrectionViewModel MyLastEdit { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(Request.Cookies["Token"]))
                return Redirect("/");

            FatalError = "";

            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var pageUrlResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/pageurl?id={Request.Query["poemId"]}");
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

                    var rhythmResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/ganjoor/rhythms?sortOnVerseCount=true");
                    if (!rhythmResponse.IsSuccessStatusCode)
                    {
                        FatalError = JsonConvert.DeserializeObject<string>(await rhythmResponse.Content.ReadAsStringAsync());
                        return Page();
                    }

                    RhythmsByVerseCount = JsonConvert.DeserializeObject<GanjoorMetre[]>(await rhythmResponse.Content.ReadAsStringAsync());

                    List<GanjoorMetre> rhythmsByVerseCount = new List<GanjoorMetre>(RhythmsByVerseCount);
                    rhythmsByVerseCount.Sort((a, b) => a.Rhythm.CompareTo(b.Rhythm));
                    rhythmsByVerseCount.Insert(0, new GanjoorMetre()
                    {
                        Rhythm = ""
                    }
                    );

                    RhythmsAlphabetically = rhythmsByVerseCount.ToArray();

                    PoemSection = PageInformation.Poem.Sections.Where(s => s.Index == int.Parse(Request.Query["index"])).Single();
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
