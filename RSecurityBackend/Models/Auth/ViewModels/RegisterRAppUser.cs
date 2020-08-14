using System.ComponentModel.DataAnnotations;

namespace RSecurityBackend.Models.Auth.ViewModels
{
    /// <summary>
    /// user infor used for registeration
    /// </summary>
    public class RegisterRAppUser : PublicRAppUser
    {
        /// <summary>
        /// desired password, if sent empty system generates one
        /// </summary>
        /// <example>
        /// Test!123
        /// </example>
        [MinLength(6)]        
        public string Password { get; set; }

        /// <summary>
        /// is admin
        /// </summary>
        /// <example>
        /// false
        /// </example>
        public bool IsAdmin { get; set; }
    }
}
