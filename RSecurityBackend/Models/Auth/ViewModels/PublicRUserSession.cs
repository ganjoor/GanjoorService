using System;

namespace RSecurityBackend.Models.Auth.ViewModels
{
    /// <summary>
    /// a safe subset of RUserSession
    /// </summary>
    public class PublicRUserSession
    {

        /// <summary>
        /// Session Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// User
        /// </summary>
        public PublicRAppUser RAppUser { get; set; }

        /// <summary>
        /// Client IP Address
        /// </summary>
        public string ClientIPAddress { get; set; }

        /// <summary>
        /// Client Application Name
        /// </summary>
        /// <example>
        /// Ganjoor Angular Client
        /// </example>
        public string ClientAppName { get; set; }

        /// <summary>
        /// Client Language
        /// </summary>
        /// <example>
        /// fa-IR
        /// </example>
        public string Language { get; set; }

        /// <summary>
        /// Login Date
        /// </summary>
        public DateTime LoginTime { get; set; }

        /// <summary>
        /// Last Renewal
        /// </summary>
        public DateTime LastRenewal { get; set; }
    }
}
