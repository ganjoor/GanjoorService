using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.FAQ;
using RMuseum.Models.Ganjoor.ViewModels;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace GanjooRazor.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class FAQModel : LoginPartialEnabledPageModel
    {
        private readonly PoetCacheService _poetCache;

        public string LastError { get; set; }

        public List<GanjoorPoetViewModel> Poets { get; set; }

        public List<FAQCategory> PinnedItemsCategories { get; set; }

        public FAQItem Question { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var maintenanceResult = TryGetMaintenanceModeResult();
            if (maintenanceResult != null)
            {
                return maintenanceResult;
            }

            ViewData["Title"] = $"گنجور » پرسش‌های متداول";
            InitializeCommonPageState();

            var (poetsOk, poets, poetsError) = await _poetCache.GetPoetsAsync(AggressiveCacheEnabled);
            if (!poetsOk)
            {
                LastError = poetsError;
                return Page();
            }
            Poets = poets;

            if (!string.IsNullOrEmpty(Request.Query["id"]))
            {
                var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/faq/{Request.Query["id"]}");
                if (!response.IsSuccessStatusCode)
                {
                    LastError = await ReadErrorMessageAsync(response);
                    if (string.IsNullOrEmpty(LastError))
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
                    LastError = await ReadErrorMessageAsync(response);
                    return Page();
                }
                PinnedItemsCategories = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<FAQCategory>>();
            }

            return Page();
        }

        /// <summary>
        /// constructor
        /// </summary>
        public FAQModel(HttpClient httpClient, IConfiguration configuration, PoetCacheService poetCache) : base(httpClient, configuration)
        {
            _poetCache = poetCache;
        }
    }
}
