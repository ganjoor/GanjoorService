using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using RSecurityBackend.Models.Auth.ViewModels;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace GanjooRazor.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class SignUpModel : PageModel
    {
        /// <summary>
        /// HttpClient instance
        /// </summary>
        private readonly HttpClient _httpClient;
        public SignUpModel(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public bool LoggedIn { get; set; }

        public string LastError { get; set; }

        public string CaptchaImageUrl { get; set; }

        [BindProperty]
        public UnverifiedSignUpViewModel SignUpViewModel { get; set; }
        public async Task<IActionResult> OnGetAsync()
        {
            LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Name"]);
            LastError = Request.Query["error"];

            SignUpViewModel = new UnverifiedSignUpViewModel();

            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/users/captchaimage");
            response.EnsureSuccessStatusCode();

            SignUpViewModel.CaptchaImageId = JsonConvert.DeserializeObject<Guid>(await response.Content.ReadAsStringAsync());

            CaptchaImageUrl = $"{APIRoot.InternetUrl}/api/rimages/{SignUpViewModel.CaptchaImageId}.jpg";

            return Page();
        }
    }
}
