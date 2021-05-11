using RSecurityBackend.Models.Auth.ViewModels;

namespace GanjooRazor.Models.User
{
    /// <summary>
    /// change password model
    /// </summary>
    public class ChangePasswordModel : SetPasswordModel
    {
        /// <summary>
        /// new password model
        /// </summary>
        public string NewPasswordRepeat { get; set; }
    }
}
