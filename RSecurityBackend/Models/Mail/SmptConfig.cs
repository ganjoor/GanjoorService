namespace RSecurityBackend.Models.Mail
{
    /// <summary>
    /// smtp config
    /// </summary>
    public class SmptConfig
    {
        /// <summary>
        /// port
        /// </summary>
        public int port { get; set; }
        /// <summary>
        /// server
        /// </summary>
        public string server { get; set; }
        /// <summary>
        /// username
        /// </summary>
        public string smtpUsername { get; set; }
        /// <summary>
        /// password
        /// </summary>
        public string smtpPassword { get; set; }
        /// <summary>
        /// from
        /// </summary>
        public string from { get; set; }

        /// <summary>
        /// use ssl
        /// </summary>
        public bool useSsl { get; set; }
    }
}
