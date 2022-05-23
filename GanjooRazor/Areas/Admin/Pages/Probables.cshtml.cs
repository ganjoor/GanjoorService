using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor;
using RMuseum.Models.Ganjoor.ViewModels;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GanjooRazor.Areas.Admin.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class ProbablesModel : PageModel
    {
        /// <summary>
        /// HttpClient instance
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// last message
        /// </summary>
        public string LastMessage { get; set; }

        /// <summary>
        /// rythm
        /// </summary>
        public List<GanjoorMetre> Rhythms { get; set; }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="httpClient"></param>
        public ProbablesModel(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

       
        public GanjoorPageCompleteViewModel PageInformation { get; set; }

        /// <summary>
        /// poem section
        /// </summary>
        [BindProperty]
        public GanjoorPoemSection PoemSection { get; set; }
        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(Request.Cookies["Token"]))
                return Redirect("/");

            LastMessage = "";

            var rhythmResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/rhythms?sortOnVerseCount=true");
            if (!rhythmResponse.IsSuccessStatusCode)
            {
                LastMessage = JsonConvert.DeserializeObject<string>(await rhythmResponse.Content.ReadAsStringAsync());
                return Page();
            }
            Rhythms = JsonConvert.DeserializeObject<List<GanjoorMetre>>(await rhythmResponse.Content.ReadAsStringAsync());

            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/probablemetre/next");
            if (!response.IsSuccessStatusCode)
            {
                LastMessage = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
            }
            else
            {
                PoemSection = JsonConvert.DeserializeObject<GanjoorPoemSection>(await response.Content.ReadAsStringAsync());
                if(string.IsNullOrEmpty(PoemSection.GanjoorMetre.Rhythm))
                {
                    Rhythms.Add
                        (
                        new GanjoorMetre()
                        {
                            Id = 0,
                            Rhythm = ""
                        }
                        );
                }

                var pageUrlResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/pageurl?id={PoemSection.PoemId}");
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
                PageInformation = JObject.Parse(await pageQuery.Content.ReadAsStringAsync()).ToObject<GanjoorPageCompleteViewModel>();

            }

            return Page();
        }


        public async Task<IActionResult> OnPostAsync(GanjoorPoemSection PoemSection)
        {
            LastMessage = "";
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    int probableRecordId = PoemSection.GanjoorMetre.Id;
                    string metre = PoemSection.GanjoorMetre.Rhythm;

                    if(string.IsNullOrEmpty(metre))
                    {
                        LastMessage = "وزنی انتخاب نشده.";
                        return Page();
                    }
                    var response = await secureClient.PutAsync($"{APIRoot.Url}/api/ganjoor/probablemetre/save/{probableRecordId}",
                        new StringContent(JsonConvert.SerializeObject(metre), Encoding.UTF8, "application/json")
                        );

                    if (!response.IsSuccessStatusCode)
                    {
                        LastMessage = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                    }
                    else
                    {
                        return Redirect($"/Admin/Probables");
                    }
                }
                else
                {
                    LastMessage = "لطفا از گنجور خارج و مجددا به آن وارد شوید.";
                }

            }
            return Page();
        }

        public async Task<IActionResult> OnPostDismissAsync(GanjoorPoemSection PoemSection)
        {
            LastMessage = "";
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    int probableRecordId = PoemSection.GanjoorMetre.Id;
                    var response = await secureClient.DeleteAsync($"{APIRoot.Url}/api/ganjoor/probablemetre/dismiss/{probableRecordId}");

                    if (!response.IsSuccessStatusCode)
                    {
                        LastMessage = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                    }
                    else
                    {
                        return Redirect($"/Admin/Probables");
                    }
                }
                else
                {
                    LastMessage = "لطفا از گنجور خارج و مجددا به آن وارد شوید.";
                }

            }
            return Page();
        }
    }
}
