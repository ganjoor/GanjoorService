using System;

namespace RSecurityBackend.Models.Auth.Db
{
    /// <summary>
    /// banned emails (previous users who had been kicked out and their emails are logged to not allow them to signup again)
    /// </summary>
    public class BannedEmail
    {
        /// <summary>
        /// id
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// all capital email
        /// </summary>
        public string NormalizedEmail { get; set; }

        /// <summary>
        /// cause
        /// </summary>
        public string Description { get; set; }
    }
}
