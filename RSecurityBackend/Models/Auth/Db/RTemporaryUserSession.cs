using System;

namespace RSecurityBackend.Models.Auth.Db
{
    /// <summary>
    /// Ganjoor User Session (Temporary)
    /// </summary>
    /// <remarks>
    /// Warning: Instances are supposed to delete when user logs out, so do not link anything serious to it
    /// </remarks>
    public class RTemporaryUserSession
    {
        /// <summary>
        /// Session Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// User Id
        /// </summary>
        public Guid RAppUserId { get; set; }

        /// <summary>
        /// User
        /// </summary>
        public RAppUser RAppUser { get; set; }

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

        /// <summary>
        /// Expiraton Time
        /// </summary>
        public DateTime ValidUntil { get; set; }

        /// <summary>
        /// User Token
        /// </summary>
        public string Token { get; set; }



    }
}
