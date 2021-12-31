using Audit.WebApi;

namespace RSecurityBackend.Models.Auth.ViewModels
{
    /// <summary>
    /// AppUserController.Login parameter
    /// </summary>
    public class LoginViewModel
    {
        /// <summary>
        /// username
        /// </summary>
        /// <example>
        /// email@domain.com
        /// </example>
        public string Username { get; set; }

        /// <summary>
        /// password
        /// </summary>
        /// <example>
        /// Test!123
        /// </example>     
        public string Password { get; set; }

        /// <summary>
        /// Client App Name
        /// </summary>
        /// <example>
        /// Swashbuckle UI Client
        /// </example>
        public string ClientAppName { get; set; }

        /// <summary>
        /// Client Language
        /// </summary>
        /// <example>
        /// fa-IR
        /// </example>
        public string Language { get; set; }
    }
}
