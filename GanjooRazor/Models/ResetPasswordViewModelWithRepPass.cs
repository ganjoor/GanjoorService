using RSecurityBackend.Models.Auth.ViewModels;

namespace GanjooRazor.Models
{
    /// <summary>
    ///  ResetPasswordViewModel with PasswordConfirmation field
    /// </summary>
    public class ResetPasswordViewModelWithRepPass : ResetPasswordViewModel
    {
        /// <summary>
        /// password confirmation
        /// </summary>
        public string PasswordConfirmation { get; set; }
    }
}
