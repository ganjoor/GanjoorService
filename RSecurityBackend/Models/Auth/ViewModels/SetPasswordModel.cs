namespace RSecurityBackend.Models.Auth.ViewModels
{
    /// <summary>
    /// set password model
    /// </summary>
    public class SetPasswordModel
    {
        /// <summary>
        /// old password
        /// </summary>
        public string OldPassword { get; set; }

        /// <summary>
        /// new password
        /// </summary>
        public string NewPassword { get; set; }
    }
}
