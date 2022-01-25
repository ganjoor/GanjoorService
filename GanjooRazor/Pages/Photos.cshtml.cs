using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RMuseum.Models.Ganjoor.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace GanjooRazor.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class PhotosModel : LoginPartialEnabledPageModel
    {
        public string LastError { get; set; }

        public List<GanjoorPoetViewModel> Poets { get; set; }

        public GanjoorPoetViewModel Poet { get; set; }

        private async Task<List<GanjoorPoetViewModel>> _PreparePoets()
        {
            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poets");
            if (!response.IsSuccessStatusCode)
            {
                LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                return new List<GanjoorPoetViewModel>();
            }
            var poets = JArray.Parse(await response.Content.ReadAsStringAsync()).ToObject<List<GanjoorPoetViewModel>>();


            foreach (var poet in poets)
            {
                poet.ImageUrl = $"{APIRoot.InternetUrl}{poet.ImageUrl}";
            }

            return poets;
        }
        public async Task<IActionResult> OnGetAsync()
        {
            LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Token"]);
           
            
            if (!string.IsNullOrEmpty(Request.Query["p"]))
            {
                var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/ganjoor/poet?url=/{Request.Query["p"]}");
                if (!response.IsSuccessStatusCode)
                {
                    LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                    return Page();
                }
                Poet = JObject.Parse(await response.Content.ReadAsStringAsync()).ToObject<GanjoorPoetCompleteViewModel>().Poet;
                Poet.ImageUrl = $"{APIRoot.InternetUrl}{Poet.ImageUrl}";
            }
            else
            {
                Poets = await _PreparePoets();
            }

            ViewData["Title"] = Poet == null ? "پیشنهاد تصویر برای شاعران" : $"پیشنهاد تصویر برای {Poet.Nickname}";

            return Page();
        }

        public PhotosModel(HttpClient httpClient) : base(httpClient)
        {
            
        }
    }
}
