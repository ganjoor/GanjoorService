using System;

namespace RSecurityBackend.Models.Auth.Db
{
    /// <summary>
    /// User SignUp Queue
    /// </summary>
    public class RVerifyQueueItem
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// queue type
        /// </summary>
        public RVerifyQueueType QueueType { get; set; }

        /// <summary>
        /// Email
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// PhoneNumber
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// DateTime
        /// </summary>
        public DateTime DateTime { get; set; }

        /// <summary>
        /// Secret
        /// </summary>
        public string Secret { get; set; }

        /// <summary>
        /// client IP address
        /// </summary>
        public string ClientIPAddress { get; set; }

        /// <summary>
        /// client app name
        /// </summary>
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
