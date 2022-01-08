
using GanjooRazor.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RMuseum.Models.Auth.Memory;
using RMuseum.Models.Auth.ViewModel;
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
    public class ResetPasswordModel : PageModel
    {
        /// <summary>
        /// HttpClient instance
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// configuration
        /// </summary>
        private readonly IConfiguration _configuration;

        public ResetPasswordModel(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }


        public bool LoggedIn { get; set; }

        public string LastError { get; set; }

        [BindProperty]
        public UnverifiedSignUpViewModel ForgotPasswordViewModel { get; set; }

        [BindProperty]
        public string Secret { get; set; }


        [BindProperty]
        public ResetPasswordViewModelWithRepPass ResetPasswordViewModel { get; set; }

        public string CaptchaImageUrl { get; set; }

        public bool PhaseSendEmail { get; set; }

        public bool PhaseVerify { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!string.IsNullOrEmpty(Request.Query["secret"]))
                return await OnPostVerifyAsync(Request.Query["secret"]);
            LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Name"]);
            LastError = "";
            PhaseSendEmail = true;
            PhaseVerify = false;

            ForgotPasswordViewModel = new UnverifiedSignUpViewModel()
            {
                ClientAppName = "وبگاه گنجور",
                Language = "fa-IR",
                CallbackUrl = $"{_configuration["SiteUrl"]}/resetpassword"
            };

            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/users/captchaimage");
            if (!response.IsSuccessStatusCode)
            {
                LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                return Page();
            }

            ForgotPasswordViewModel.CaptchaImageId = JsonConvert.DeserializeObject<Guid>(await response.Content.ReadAsStringAsync());

            CaptchaImageUrl = $"{APIRoot.InternetUrl}/api/rimages/{ForgotPasswordViewModel.CaptchaImageId}.jpg";

            return Page();
        }

        public async Task<IActionResult> OnPostSendEmailAsync(UnverifiedSignUpViewModel forgotPasswordViewModel)
        {
            LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Name"]);
            LastError = "";
            PhaseSendEmail = true;
            PhaseVerify = false;

            var response = await _httpClient.PostAsync($"{APIRoot.Url}/api/users/forgotpassword", new StringContent(JsonConvert.SerializeObject(ForgotPasswordViewModel), Encoding.UTF8, "application/json"));
            if (!response.IsSuccessStatusCode)
            {
                LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());

                response = await _httpClient.GetAsync($"{APIRoot.Url}/api/users/captchaimage");
                if (!response.IsSuccessStatusCode)
                {
                    LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                    return Page();
                }

                ModelState.Clear();

                ForgotPasswordViewModel = new UnverifiedSignUpViewModel()
                {
                    ClientAppName = forgotPasswordViewModel.ClientAppName,
                    Language = forgotPasswordViewModel.Language,
                    CallbackUrl = forgotPasswordViewModel.CallbackUrl,
                    Email = forgotPasswordViewModel.Email
                };

                ForgotPasswordViewModel.CaptchaImageId = JsonConvert.DeserializeObject<Guid>(await response.Content.ReadAsStringAsync());
                CaptchaImageUrl = $"{APIRoot.InternetUrl}/api/rimages/{ForgotPasswordViewModel.CaptchaImageId}.jpg";


                return Page();
            }
            PhaseSendEmail = false;
            PhaseVerify = true;


            return Page();
        }

        public async Task<IActionResult> OnPostVerifyAsync(string Secret)
        {
            LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Name"]);
            LastError = "";
            PhaseSendEmail = false;
            PhaseVerify = true;

            var response = await _httpClient.GetAsync($"{APIRoot.Url}/api/users/verify?type=1&secret={Secret}");
            if (!response.IsSuccessStatusCode)
            {
                LastError = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                return Page();
            }

            ResetPasswordViewModel = new ResetPasswordViewModelWithRepPass()
            {
                Secret = Secret,
                Email = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync()),
                Password = "",
                PasswordConfirmation = ""
            };

            PhaseVerify = false;


            return Page();
        }

        public async Task<IActionResult> OnPostPhase3Async()
        {
            LoggedIn = !string.IsNullOrEmpty(Request.Cookies["Name"]);
            LastError = "";
            PhaseSendEmail = false;
            PhaseVerify = false;

            if (ResetPasswordViewModel.Password != ResetPasswordViewModel.PasswordConfirmation)
            {
                LastError = "گذرواژه و تکرار آن یکی نیستند.";
                return Page();
            }

            ResetPasswordViewModel postViewModel = new ResetPasswordViewModel()
            {
                Email = ResetPasswordViewModel.Email,
                Secret = ResetPasswordViewModel.Secret,
                Password = ResetPasswordViewModel.Password
            };

            var response = await _httpClient.PostAsync($"{APIRoot.Url}/api/users/resetpassword", new StringContent(JsonConvert.SerializeObject(postViewModel), Encoding.UTF8, "application/json"));
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

            LoggedOnUserModelEx loggedOnUser = JsonConvert.DeserializeObject<LoggedOnUserModelEx>(await response.Content.ReadAsStringAsync());

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
            Response.Cookies.Append("KeepHistory", $"{loggedOnUser.KeepHistory}", cookieOption);

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
