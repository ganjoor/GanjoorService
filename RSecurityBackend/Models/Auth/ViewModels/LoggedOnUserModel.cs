using RSecurityBackend.Models.Auth.Memory;
using System;

namespace RSecurityBackend.Models.Auth.ViewModels
{
    /// <summary>
    /// Logged On User Model
    /// </summary>
    public class LoggedOnUserModel
    {
        /// <summary>
        /// Session Id
        /// </summary>
        public Guid SessionId { get; set; }

        /// <summary>
        /// Security Token
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// User Information
        /// </summary>
        public PublicRAppUser User { get; set; }

        /// <summary>
        /// Permissions
        /// </summary>
        public SecurableItem[] SecurableItem { get; set; }

    }
}
