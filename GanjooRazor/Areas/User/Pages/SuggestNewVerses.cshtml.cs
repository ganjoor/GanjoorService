using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using RMuseum.Models.Ganjoor.ViewModels;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System;

namespace GanjooRazor.Areas.User.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class SuggestNewVersesModel : PageModel
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
        public SuggestNewVersesModel(HttpClient httpClient, IConfiguration configuration)
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

        public int  CoupletIndex { get; set; }

        public string[] NewLines { get; set; }

        /// <summary>
        /// Couplets
        /// </summary>
        public Tuple<int, string>[] Couplets { get; set; }

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


                int coupetIndex = -1;
                string coupletText = "";
                List<Tuple<int, string>> couplets = new List<Tuple<int, string>>
                {
                    new Tuple<int, string>(-1, "*")
                };
                foreach (var verse in PageInformation.Poem.Verses)
                {
                    if (verse.CoupletIndex != null && verse.CoupletIndex != coupetIndex && verse.CoupletIndex >= 0)
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
                    //var stringContent = new StringContent(JsonConvert.SerializeObject(Report), Encoding.UTF8, "application/json");
                    var methodUrl = $"{APIRoot.Url}/api/audio/errors/report";
                    var response = await _httpClient.PostAsync(methodUrl, null /*stringContent*/);
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
                    LastError = "لطفاً از گنجور خارج و مجددا به آن وارد شوید.";
                }
            }

            return Page();
        }
    }
}
