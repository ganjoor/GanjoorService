using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor.ViewModels;
using RMuseum.Models.GanjoorAudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GanjooRazor.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class ReportRecitationModel : PageModel
    {
        // <summary>
        /// HttpClient instance
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="httpClient"></param>
        public ReportRecitationModel(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }


        /// <summary>
        /// is logged on
        /// </summary>
        public bool LoggedIn { get; set; }

        /// <summary>
        /// Last Error
        /// </summary>
        public string LastError { get; set; }

        /// <summary>
        /// Post Success
        /// </summary>
        public bool PostSuccess { get; set; }

        /// <summary>
        /// Recitation Info
        /// </summary>
        public string RecitationInfo { get; set; }

        /// <summary>
        /// Couplets
        /// </summary>
        public Tuple<int, string>[] Couplets { get; set; }

        /// <summary>
        /// api model
        /// </summary>
        [BindProperty]
        public RecitationErrorReportViewModel Report { get; set; }
        public async Task<IActionResult> OnGetAsync()
        {
            PostSuccess = false;
            LastError = "";
            LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Token"]);
            RecitationInfo = "";

            Report = new RecitationErrorReportViewModel()
            {
                ReasonText = "",
                NumberOfLinesAffected = 1,
                CoupletIndex = -1,
            };

            if (!string.IsNullOrEmpty(Request.Query["a"]))
            {
                Report.RecitationId = int.Parse(Request.Query["a"]);
                var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/audio/published/{Report.RecitationId}");
                if (!response.IsSuccessStatusCode)
                {
                    LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                }
                else
                {
                    var recitation = JsonConvert.DeserializeObject<PublicRecitationViewModel>(await response.Content.ReadAsStringAsync());
                    RecitationInfo = $"{recitation.AudioTitle} به خوانش {recitation.AudioArtist}";

                    var pageUrlResponse = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/pageurl?id={recitation.PoemId}");
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
                    var pageInformation = JObject.Parse(await pageQuery.Content.ReadAsStringAsync()).ToObject<GanjoorPageCompleteViewModel>();


                    int coupetIndex = -1;
                    string coupletText = "";
                    List<Tuple<int, string>> couplets = new List<Tuple<int, string>>();
                    couplets.Add(new Tuple<int, string>(-1, "*"));
                    foreach (var verse in pageInformation.Poem.Verses)
                    {
                        if(verse.CoupletIndex != null && verse.CoupletIndex != coupetIndex && verse.CoupletIndex >= 0)
                        {
                            if (!string.IsNullOrEmpty(coupletText))
                                couplets.Add(new Tuple<int, string>(coupetIndex, coupletText));
                            coupetIndex = (int)verse.CoupletIndex;
                            coupletText = "";
                        }
                        if (!string.IsNullOrEmpty(coupletText))
                            coupletText += " ";
                        coupletText += verse.Text;
                    }

                    if (!string.IsNullOrEmpty(coupletText))
                        couplets.Add(new Tuple<int, string>(coupetIndex, coupletText));

                    Couplets = couplets.ToArray();

                }
            }
            else
            {
                Report.RecitationId = 0;
            }
           
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            PostSuccess = false;
            LastError = "";
            LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Token"]);
            RecitationInfo = "";

            if (string.IsNullOrEmpty(Report.ReasonText))
            {
                LastError = "مشکل مشخص نشده است. ";
                return Page();
            }

            Report.ReasonText = Report.ReasonText.Trim();

            if (string.IsNullOrEmpty(Report.ReasonText))
            {
                LastError = "مشکل مشخص نشده است. ";
                return Page();
            }

            if (Report.NumberOfLinesAffected < 1)
                Report.NumberOfLinesAffected = 1;

            using (HttpClient _httpClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(_httpClient, Request, Response))
                {
                    var stringContent = new StringContent(JsonConvert.SerializeObject(Report), Encoding.UTF8, "application/json");
                    var methodUrl = $"{APIRoot.Url}/api/audio/errors/report";
                    var response = await _httpClient.PostAsync(methodUrl, stringContent);
                    if (!response.IsSuccessStatusCode)
                    {
                        LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                    }
                    else
                    {
                        PostSuccess = true;
                    }
                }
                else
                {
                    LastError = "لطفا از گنجور خارج و مجددا به آن وارد شوید.";
                }
            }

            return Page();
        }
    }
}
