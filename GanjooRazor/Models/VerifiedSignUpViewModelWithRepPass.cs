using RSecurityBackend.Models.Auth.ViewModels;

namespace GanjooRazor.Models
{
    /// <summary>
    /// erifiedSignUpViewModel with PasswordConfirmation
    /// </summary>
    public class VerifiedSignUpViewModelWithRepPass : VerifiedSignUpViewModel
    {
        /// <summary>
        /// password confirmation
        /// </summary>
        public string PasswordConfirmation { get; set; }
    }
}
