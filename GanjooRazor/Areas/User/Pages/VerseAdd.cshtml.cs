using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor.ViewModels;
using System.Net.Http;
using System.Threading.Tasks;
using GanjooRazor.Models;
using RMuseum.Models.Ganjoor;
using System.Collections.Generic;
using System;
using System.Text;
using System.Linq;

namespace GanjooRazor.Areas.User.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class VerseAddPageModel : PageModel
    {
        /// <summary>
        /// HttpClient instance
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// configration file reader (appsettings.json)
        /// </summary>
        private readonly IConfiguration Configuration;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="configuration"></param>
        public VerseAddPageModel(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            Configuration = configuration;
        }

        /// <summary>
        /// is logged on
        /// </summary>
        public bool LoggedIn { get; set; }

        /// <summary>
        /// Last Error
        /// </summary>
        public string LastError { get; set; }

        public bool PostSuccess { get; set; }

        [BindProperty]
        public NewVersesModel NewVerses { get; set; }

        public GanjoorPageCompleteViewModel PageInformation { get; set; }


        public async Task<IActionResult> OnGetAsync()
        {
            if (bool.Parse(Configuration["MaintenanceMode"]))
            {
                return StatusCode(503);
            }

            PostSuccess = false;
            LastError = "";
            LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Token"]);


            if (!string.IsNullOrEmpty(Request.Query["id"]))
            {
                var pageUrlResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/pageurl?id={Request.Query["id"]}");
                if (!pageUrlResponse.IsSuccessStatusCode)
                {
                    LastError = JsonConvert.DeserializeObject<string>(await pageUrlResponse.Content.ReadAsStringAsync());
                    return Page();
                }
                var pageUrl = JsonConvert.DeserializeObject<string>(await pageUrlResponse.Content.ReadAsStringAsync());

                var pageQuery = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/page?url={pageUrl}");
                if (!pageQuery.IsSuccessStatusCode)
                {
                    LastError = JsonConvert.DeserializeObject<string>(await pageQuery.Content.ReadAsStringAsync());
                    return Page();
                }
                PageInformation = JObject.Parse(await pageQuery.Content.ReadAsStringAsync()).ToObject<GanjoorPageCompleteViewModel>();

                NewVerses = new NewVersesModel()
                {
                    PoemId = PageInformation.Id,
                    VOrder = 0,
                    Lines = "",
                };
                
            }
            else
            {
                LastError = "missing parameter: id";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {

            LastError = "";
            LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Token"]);


            using (HttpClient _httpClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(_httpClient, Request, Response))
                {
                    var pageUrlResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/pageurl?id={NewVerses.PoemId}");
                    if (!pageUrlResponse.IsSuccessStatusCode)
                    {
                        LastError = JsonConvert.DeserializeObject<string>(await pageUrlResponse.Content.ReadAsStringAsync());
                        return Page();
                    }
                    var pageUrl = JsonConvert.DeserializeObject<string>(await pageUrlResponse.Content.ReadAsStringAsync());

                    var pageQuery = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/page?url={pageUrl}");
                    if (!pageQuery.IsSuccessStatusCode)
                    {
                        LastError = JsonConvert.DeserializeObject<string>(await pageQuery.Content.ReadAsStringAsync());
                        return Page();
                    }
                    PageInformation = JObject.Parse(await pageQuery.Content.ReadAsStringAsync()).ToObject<GanjoorPageCompleteViewModel>();
                    VersePosition versePosition = VersePosition.Right;
                    if(PageInformation.Poem.Verses.Any(v => v.VOrder == NewVerses.VOrder))
                    {  
                        if (
                            PageInformation.Poem.Verses.Single(v => v.VOrder == NewVerses.VOrder).VersePosition == VersePosition.Paragraph
                            ||
                            PageInformation.Poem.Verses.Single(v => v.VOrder == NewVerses.VOrder).VersePosition == VersePosition.Single
                            )
                        {
                            versePosition = PageInformation.Poem.Verses.Single(v => v.VOrder == NewVerses.VOrder).VersePosition;
                        }
                    }

                    List<GanjoorVerseVOrderText> vOrderTexts = new List<GanjoorVerseVOrderText>();

                    int vOrderNext = 0;
                    foreach (string v in NewVerses.Lines.Split(new char[] { '\r', '\n'}, StringSplitOptions.RemoveEmptyEntries))
                    {
                        vOrderTexts.Add
                                (
                                new GanjoorVerseVOrderText()
                                {
                                    VORder = NewVerses.VOrder + vOrderNext,
                                    Text = v.Replace("ۀ", "هٔ").Replace("ك", "ک"),
                                    NewVerse= true,
                                    VersePosition = versePosition,
                                }
                                );
                        if(versePosition != VersePosition.Paragraph && versePosition != VersePosition.Single)
                        {
                            versePosition = versePosition == VersePosition.Right ? VersePosition.Left : VersePosition.Right;
                        }
                        vOrderNext++;
                    }
                    GanjoorPoemCorrectionViewModel correction = new GanjoorPoemCorrectionViewModel()
                    {
                        PoemId = NewVerses.PoemId,
                        VerseOrderText = vOrderTexts.ToArray(),
                        Note = "پیشنهاد مصرع‌های جاافتاده"
                    };
                    var stringContent = new StringContent(JsonConvert.SerializeObject(correction), Encoding.UTF8, "application/json");
                    var methodUrl = $"{APIRoot.Url}/api/ganjoor/poem/correction";
                    var response = await _httpClient.PostAsync(methodUrl, stringContent);
                    if (!response.IsSuccessStatusCode)
                    {
                        LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                    }
                    else
                    {
                        PostSuccess = true;
                        return Redirect($"/User/Editor?id={PageInformation.Id}");
                    }
                }
                else
                {
                    LastError = "لطفاً از گنجور خارج و مجددا به آن وارد شوید.";
                }
            }

            return Page();
        }
    }
}
