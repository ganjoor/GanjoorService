using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RSecurityBackend.Models.Auth.ViewModels;

namespace GanjooRazor.Pages
{
    public class LoginPartialEnabledPageModel : PageModel
    {
        /// <summary>
        /// is logged on
        /// </summary>
        public bool LoggedIn { get; set; }

        [BindProperty]
        public LoginViewModel LoginViewModel { get; set; }
    }
}
