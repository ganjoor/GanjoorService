namespace RSecurityBackend.Models.Auth.ViewModels
{
    /// <summary>
    /// User Self Delete View Model
    /// </summary>
    public class SelfDeleteViewModel
    {
        /// <summary>
        /// user password
        /// </summary>
        /// <example>
        /// Test!123
        /// </example>     
        public string Password { get; set; }

        /// <summary>
        ///CallbackUrl
        /// </summary>
        public string CallbackUrl { get; set; }
    }
}
