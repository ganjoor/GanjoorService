using System;

namespace RSecurityBackend.Models.Auth.ViewModels
{
    /// <summary>
    /// SignUp View Model
    /// </summary>
    public class UnverifiedSignUpViewModel
    {
        /// <summary>
        /// Email
        /// </summary>
        /// <example>
        /// email@domain.com
        /// </example>
        public string Email { get; set; }

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

        /// <summary>
        /// Captcha Image Id
        /// </summary>
        public Guid CaptchaImageId { get; set; }

        /// <summary>
        /// Captcha Value
        /// </summary>
        public string CaptchaValue { get; set; }

        /// <summary>
        ///CallbackUrl
        /// </summary>
        public string CallbackUrl { get; set; }
    }
}
