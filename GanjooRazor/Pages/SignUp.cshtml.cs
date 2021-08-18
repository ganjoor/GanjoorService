using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using RSecurityBackend.Models.Auth.ViewModels;
using System;
using System.Net.Http;
using System.Text;
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


        public bool SignupPhase1 { get; set; }

        public bool SignupVerifyEmailPhase { get; set; }

        public bool SignupFinalPhase { get; set; }

        [BindProperty]
        public UnverifiedSignUpViewModel SignUpViewModel { get; set; }

        [BindProperty]
        public string Secret { get; set; }
        public async Task<IActionResult> OnGetAsync()
        {
            LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Name"]);
            LastError = "";
            SignupPhase1 = true;
            SignupVerifyEmailPhase = false;
            SignupFinalPhase = false;

            SignUpViewModel = new UnverifiedSignUpViewModel()
            {
                ClientAppName = "وبگاه گنجور",
                Language = "fa-IR",
                CallbackUrl = "https://ganjoor.net/signup"
            };

            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/users/captchaimage");
            response.EnsureSuccessStatusCode();

            SignUpViewModel.CaptchaImageId = JsonConvert.DeserializeObject<Guid>(await response.Content.ReadAsStringAsync());

            CaptchaImageUrl = $"{APIRoot.InternetUrl}/api/rimages/{SignUpViewModel.CaptchaImageId}.jpg";

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(UnverifiedSignUpViewModel SignUpViewModel)
        {
            LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Name"]);
            LastError = "";
            SignupPhase1 = true;
            SignupVerifyEmailPhase = false;
            SignupFinalPhase = false;

            var response = await _httpClient.PostAsync($"{APIRoot.Url}/api/users/signup", new StringContent(JsonConvert.SerializeObject(SignUpViewModel), Encoding.UTF8, "application/json"));
            if(!response.IsSuccessStatusCode)
            {
                LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                return Page();
            }
            SignupPhase1 = false;
            SignupVerifyEmailPhase = true;


            return Page();
        }
    }
}
