using GanjooRazor.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RMuseum.Models.Auth.Memory;
using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Models.Auth.ViewModels;
using System;
using System.Linq;
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

        /// <summary>
        /// configuration
        /// </summary>
        private readonly IConfiguration _configuration;
        public SignUpModel(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
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

        [BindProperty]
        public VerifiedSignUpViewModelWithRepPass FinalViewModel { get; set; }
        public async Task<IActionResult> OnGetAsync()
        {
            if (!string.IsNullOrEmpty(Request.Query["secret"]))
                return await OnPostPhase2Async(Request.Query["secret"]);
            LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Name"]);
            LastError = "";
            SignupPhase1 = true;
            SignupVerifyEmailPhase = false;
            SignupFinalPhase = false;

            SignUpViewModel = new UnverifiedSignUpViewModel()
            {
                ClientAppName = "وبگاه گنجور",
                Language = "fa-IR",
                CallbackUrl = $"{_configuration["SiteUrl"]}/signup"
            };

            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/users/captchaimage");
            response.EnsureSuccessStatusCode();

            SignUpViewModel.CaptchaImageId = JsonConvert.DeserializeObject<Guid>(await response.Content.ReadAsStringAsync());

            CaptchaImageUrl = $"{APIRoot.InternetUrl}/api/rimages/{SignUpViewModel.CaptchaImageId}.jpg";

            return Page();
        }

        public async Task<IActionResult> OnPostPhase1Async(UnverifiedSignUpViewModel signUpViewModel)
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

                response = await _httpClient.GetAsync($"{APIRoot.Url}/api/users/captchaimage");
                response.EnsureSuccessStatusCode();

                ModelState.Clear();

                SignUpViewModel = new UnverifiedSignUpViewModel()
                {
                    ClientAppName = signUpViewModel.ClientAppName,
                    Language = signUpViewModel.Language,
                    CallbackUrl = signUpViewModel.CallbackUrl,
                    Email = signUpViewModel.Email
                };

                SignUpViewModel.CaptchaImageId = JsonConvert.DeserializeObject<Guid>(await response.Content.ReadAsStringAsync());
                CaptchaImageUrl = $"{APIRoot.InternetUrl}/api/rimages/{SignUpViewModel.CaptchaImageId}.jpg";

                
                return Page();
            }
            SignupPhase1 = false;
            SignupVerifyEmailPhase = true;


            return Page();
        }

        public async Task<IActionResult> OnPostPhase2Async(string Secret)
        {
            LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Name"]);
            LastError = "";
            SignupPhase1 = false;
            SignupVerifyEmailPhase = true;
            SignupFinalPhase = false;

            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/users/verify?type=0&secret={Secret}");
            if (!response.IsSuccessStatusCode)
            {
                LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                return Page();
            }

            FinalViewModel = new VerifiedSignUpViewModelWithRepPass()
            {
                Secret = Secret,
                Email = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()),
                FirstName = "",
                SureName = "",
                Password = "",
                PasswordConfirmation = ""
            };

            SignupVerifyEmailPhase = false;
            SignupFinalPhase = true;

            return Page();
        }

        public async Task<IActionResult> OnPostPhase3Async()
        {
            LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Name"]);
            LastError = "";
            SignupPhase1 = false;
            SignupVerifyEmailPhase = false;
            SignupFinalPhase = true;

            if(FinalViewModel.Password != FinalViewModel.PasswordConfirmation)
            {
                LastError = "گذرواژه و تکرار آن یکی نیستند.";
                return Page();
            }

            VerifiedSignUpViewModel postViewModel = new VerifiedSignUpViewModel()
            {
                Email = FinalViewModel.Email,
                Secret = FinalViewModel.Secret,
                FirstName = FinalViewModel.FirstName,
                SureName = FinalViewModel.SureName,
                Password = FinalViewModel.Password
            };

            var response = await _httpClient.PostAsync($"{APIRoot.Url}/api/users/finalizesignup", new StringContent(JsonConvert.SerializeObject(postViewModel), Encoding.UTF8, "application/json"));
            if (!response.IsSuccessStatusCode)
            {
                LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                return Page();
            }


            LoginViewModel loginViewModel = new LoginViewModel()
            {
                ClientAppName = "وبگاه گنجور",
                Language = "fa-IR",
                Username = postViewModel.Email,
                Password = postViewModel.Password
            };

            var stringContent = new StringContent(JsonConvert.SerializeObject(loginViewModel), Encoding.UTF8, "application/json");
            var loginUrl = $"{APIRoot.Url}/api/users/login";
            response = await _httpClient.PostAsync(loginUrl, stringContent);

            if (!response.IsSuccessStatusCode)
            {
                LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                return Page();
            }

            LoggedOnUserModel loggedOnUser = JsonConvert.DeserializeObject<LoggedOnUserModel>(await response.Content.ReadAsStringAsync());

            var cookieOption = new CookieOptions()
            {
                Expires = DateTime.Now.AddDays(365),
            };

            Response.Cookies.Append("UserId", loggedOnUser.User.Id.ToString(), cookieOption);
            Response.Cookies.Append("SessionId", loggedOnUser.SessionId.ToString(), cookieOption);
            Response.Cookies.Append("Token", loggedOnUser.Token, cookieOption);
            Response.Cookies.Append("Username", loggedOnUser.User.Username, cookieOption);
            Response.Cookies.Append("Name", $"{loggedOnUser.User.FirstName} {loggedOnUser.User.SureName}", cookieOption);
            Response.Cookies.Append("NickName", $"{loggedOnUser.User.NickName}", cookieOption);

            bool canEditContent = false;
            var ganjoorEntity = loggedOnUser.SecurableItem.Where(s => s.ShortName == RMuseumSecurableItem.GanjoorEntityShortName).SingleOrDefault();
            if (ganjoorEntity != null)
            {
                var op = ganjoorEntity.Operations.Where(o => o.ShortName == SecurableItem.ModifyOperationShortName).SingleOrDefault();
                if (op != null)
                {
                    canEditContent = op.Status;
                }
            }

            Response.Cookies.Append("CanEdit", canEditContent.ToString(), cookieOption);



            return Redirect($"{_configuration["SiteUrl"]}/User");
        }
    }
}
