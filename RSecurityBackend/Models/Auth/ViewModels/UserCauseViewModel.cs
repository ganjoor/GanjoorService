using System;

namespace RSecurityBackend.Models.Auth.ViewModels
{
    /// <summary>
    /// User/Cause used for logging a user (bad) behaviour or kicking out him or her
    /// </summary>
    public class UserCauseViewModel
    {
        /// <summary>
        /// User Id
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// cause
        /// </summary>
        public string Cause { get; set; }
    }
}
