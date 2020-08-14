using RSecurityBackend.Models.Image;
using System;

namespace RSecurityBackend.Models.Auth.Db
{
    /// <summary>
    /// Captcha Image
    /// </summary>
    public class RCaptchaImage
    {
        /// <summary>
        /// Id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Captcha Image
        /// </summary>
        public RImage RImage { get; set; }

        /// <summary>
        /// Captcha Image Id
        /// </summary>
        public Guid RImageId { get; set; }

        /// <summary>
        /// Captcha Text
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// DateTime
        /// </summary>
        public DateTime DateTime { get; set; }
    }
}
