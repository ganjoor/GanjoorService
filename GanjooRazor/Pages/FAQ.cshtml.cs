using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.FAQ;
using RMuseum.Models.Ganjoor.ViewModels;
using RSecurityBackend.Models.Auth.ViewModels;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace GanjooRazor.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class FAQModel : LoginPartialEnabledPageModel
    {
        public string LastError { get; set; }

        public List<GanjoorPoetViewModel> Poets { get; set; }

        public List<FAQCategory> PinnedItemsCategories { get; set; }

        public FAQItem Question { get; set; }

        private async Task<bool> preparePoets()
        {
            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poets");
            if (!response.IsSuccessStatusCode)
            {
                LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                return false;
            }
            Poets = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoetViewModel>>();
            
            return true;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["Title"] = $"گنجور » پرسش‌های متداول";
            LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Token"]);


            ViewData["GoogleAnalyticsCode"] = Configuration["GoogleAnalyticsCode"];

            //todo: use html master layout or make it partial
            if (false == (await preparePoets()))
                return Page();

            if(!string.IsNullOrEmpty(Request.Query["id"]))
            {
                var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/faq/{Request.Query["id"]}");
                if (!response.IsSuccessStatusCode)
                {
                    LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                    if(string.IsNullOrEmpty(LastError))
                    {
                        LastError = $"خطا در دریافت اطلاعات پرسش مد نظر - کد خطا = {response.StatusCode}";
                    }
                    return Page();
                }
                Question = JsonConvert.DeserializeObject<FAQItem>(await response.Content.ReadAsStringAsync());
                ViewData["Title"] += $" » {Question.Question}";
            }
            else
            {
                var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/faq/pinned");
                if (!response.IsSuccessStatusCode)
                {
                    LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                    return Page();
                }
                PinnedItemsCategories = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<FAQCategory>>();
            }

            return Page();
        }

        /// <summary>
        /// configration file reader (appsettings.json)
        /// </summary>
        private readonly IConfiguration Configuration;


        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="configuration"></param>
        public FAQModel(HttpClient httpClient, IConfiguration configuration) : base(httpClient)
        {
            Configuration
                = configuration;
        }
    }
}
