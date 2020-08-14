namespace RSecurityBackend.Models.Auth.ViewModels
{
    /// <summary>
    /// a model suitable for anonymous user sign up by billing users
    /// </summary>
    public class BillingUserSignup : RegisterRAppUser
    {
        /// <summary>
        /// secret for securing signup process
        /// </summary>
        public string TenantSecret { get; set; }
    }
}
