
using Microsoft.AspNetCore.Mvc.RazorPages;
using RSecurityBackend.Models.Auth.ViewModels;

namespace GanjooRazor.Pages
{
    public class ResetPasswordModel : PageModel
    {
        public bool LoggedIn { get; set; }

        public string LastError { get; set; }

        public UnverifiedSignUpViewModel SignUpViewModel { get; set; }

        public string CaptchaImageUrl { get; set; }
        public void OnGet()
        {
        }
    }
}
